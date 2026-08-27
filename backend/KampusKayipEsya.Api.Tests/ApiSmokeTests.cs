using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace KampusKayipEsya.Api.Tests;

[Collection("ApiSmoke")]
public class ApiSmokeTests
{
    private static readonly string Marker = $"smoke-{Guid.NewGuid():N}";

    private static readonly string[] ExpectedLocations =
    [
        "merkez", "kütüphane", "yemekhane", "mühendislik", "yurt", "spor salonu"
    ];

    private static readonly string[] ExpectedCategories =
    [
        "öğrenci kartı", "anahtar", "telefon", "çanta", "kıyafet", "kulaklık", "diğer"
    ];

    [Fact]
    public async Task Seed_GetItems_WithoutFilters_ReturnsSixToEightCampusListings()
    {
        using var client = ApiClient.NewClient();
        using var response = await client.GetAsync("api/items");
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 from GET /api/items, got {(int)response.StatusCode}: {ApiClient.Excerpt(body)}");

        var items = JsonSerializer.Deserialize<List<JsonElement>>(body, ApiClient.JsonOptions);
        Assert.NotNull(items);
        Assert.True(items!.Count >= 6,
            $"Expected at least 6 seeded items on a fresh DB, got {items.Count}: {ApiClient.Excerpt(body)}");

        Assert.Contains(items, i => GetString(i, "kind") == "lost");
        Assert.Contains(items, i => GetString(i, "kind") == "found");
        Assert.All(items, AssertContactPolicy);
    }

    [Fact]
    public async Task Categories_Get_ReturnsFixedList()
    {
        using var client = ApiClient.NewClient();
        using var response = await client.GetAsync("api/categories");
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 from GET /api/categories, got {(int)response.StatusCode}: {ApiClient.Excerpt(body)}");

        var categories = JsonSerializer.Deserialize<List<string>>(body, ApiClient.JsonOptions);
        Assert.NotNull(categories);
        Assert.Equal(ExpectedCategories, categories);
    }

    [Fact]
    public async Task Locations_Get_ReturnsFixedList()
    {
        using var client = ApiClient.NewClient();
        using var response = await client.GetAsync("api/locations");
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 from GET /api/locations, got {(int)response.StatusCode}: {ApiClient.Excerpt(body)}");

        var locations = JsonSerializer.Deserialize<List<string>>(body, ApiClient.JsonOptions);
        Assert.NotNull(locations);
        Assert.Equal(ExpectedLocations, locations);
    }

    [Fact]
    public async Task Crud_PostGetPutDelete_RoundTrip()
    {
        using var client = ApiClient.NewClient();
        var created = new
        {
            title = $"{Marker}-crud-title",
            description = "CRUD description",
            location = "yurt",
            category = "anahtar",
            contact = "crud@example.com",
            photoUrl = "https://example.com/crud.png",
            kind = "lost",
            status = "open"
        };

        using (var post = await client.PostAsync("api/items", ApiClient.JsonBody(created)))
        {
            var postBody = await post.Content.ReadAsStringAsync();
            Assert.True(post.StatusCode == HttpStatusCode.Created,
                $"Expected 201 from POST /api/items, got {(int)post.StatusCode}: {ApiClient.Excerpt(postBody)}");

            var posted = JsonSerializer.Deserialize<JsonElement>(postBody, ApiClient.JsonOptions);
            var id = ReadId(posted, $"POST 201 body missing id: {ApiClient.Excerpt(postBody)}");
            AssertFieldsMatch(posted, created);
            AssertStatusHistory(posted, "open");

            using (var get = await client.GetAsync($"api/items/{id}"))
            {
                var getBody = await get.Content.ReadAsStringAsync();
                Assert.True(get.StatusCode == HttpStatusCode.OK,
                    $"Expected 200 from GET /api/items/{id}, got {(int)get.StatusCode}: {ApiClient.Excerpt(getBody)}");
                var got = JsonSerializer.Deserialize<JsonElement>(getBody, ApiClient.JsonOptions);
                AssertFieldsMatch(got, created);
                AssertStatusHistory(got, "open");
            }

            var updated = new
            {
                title = $"{Marker}-crud-updated",
                description = "Updated description",
                location = "kütüphane",
                category = "telefon",
                contact = "updated@example.com",
                photoUrl = "https://example.com/updated.png",
                kind = "found",
                status = "claimed"
            };

            using (var put = await client.PutAsync($"api/items/{id}", ApiClient.JsonBody(updated)))
            {
                var putBody = await put.Content.ReadAsStringAsync();
                Assert.True(put.StatusCode == HttpStatusCode.OK,
                    $"Expected 200 from PUT /api/items/{id}, got {(int)put.StatusCode}: {ApiClient.Excerpt(putBody)}");
                var putItem = JsonSerializer.Deserialize<JsonElement>(putBody, ApiClient.JsonOptions);
                AssertFieldsMatch(putItem, updated);
                AssertStatusHistory(putItem, "open", "claimed");
            }

            using (var del = await client.DeleteAsync($"api/items/{id}"))
            {
                var delBody = await del.Content.ReadAsStringAsync();
                Assert.True(del.StatusCode == HttpStatusCode.NoContent,
                    $"Expected 204 from DELETE /api/items/{id}, got {(int)del.StatusCode}: {ApiClient.Excerpt(delBody)}");
            }

            using (var getGone = await client.GetAsync($"api/items/{id}"))
            {
                var goneBody = await getGone.Content.ReadAsStringAsync();
                Assert.True(getGone.StatusCode == HttpStatusCode.NotFound,
                    $"Expected 404 after DELETE, got {(int)getGone.StatusCode}: {ApiClient.Excerpt(goneBody)}");
            }
        }
    }

    [Fact]
    public async Task Filters_Q_Category_Location_Status_Kind_AndCombined()
    {
        using var client = ApiClient.NewClient();
        var uniqueQ = $"{Marker}-needle-q";

        var lostOpen = await CreateItemAsync(client, new
        {
            title = $"{Marker}-lost-open",
            description = uniqueQ,
            location = "yurt",
            category = "anahtar",
            contact = "filter@example.com",
            photoUrl = "https://example.com/f1.png",
            kind = "lost",
            status = "open"
        });

        var foundClaimed = await CreateItemAsync(client, new
        {
            title = $"{Marker}-found-claimed",
            description = "other description",
            location = "yemekhane",
            category = "çanta",
            contact = "filter2@example.com",
            photoUrl = "https://example.com/f2.png",
            kind = "found",
            status = "claimed"
        });

        await AssertFilterAsync(client, $"api/items?q={Uri.EscapeDataString(uniqueQ)}", lostOpen, item =>
            ContainsIgnoreCase(GetString(item, "title"), uniqueQ)
            || ContainsIgnoreCase(GetString(item, "description"), uniqueQ)
            || ContainsIgnoreCase(GetString(item, "location"), uniqueQ));

        await AssertFilterAsync(client, $"api/items?category={Uri.EscapeDataString("anahtar")}&q={Uri.EscapeDataString($"{Marker}-lost-open")}", lostOpen, item =>
            string.Equals(GetString(item, "category"), "anahtar", StringComparison.OrdinalIgnoreCase));

        await AssertFilterAsync(client, $"api/items?location={Uri.EscapeDataString("yurt")}&q={Uri.EscapeDataString($"{Marker}-lost-open")}", lostOpen, item =>
            string.Equals(GetString(item, "location"), "yurt", StringComparison.OrdinalIgnoreCase));

        await AssertFilterAsync(client, "api/items?status=claimed", foundClaimed, item =>
            string.Equals(GetString(item, "status"), "claimed", StringComparison.OrdinalIgnoreCase));

        await AssertFilterAsync(client, "api/items?kind=found", foundClaimed, item =>
            string.Equals(GetString(item, "kind"), "found", StringComparison.OrdinalIgnoreCase));

        await AssertFilterAsync(client, "api/items?kind=found&status=claimed&location=yemekhane", foundClaimed, item =>
            string.Equals(GetString(item, "kind"), "found", StringComparison.OrdinalIgnoreCase)
            && string.Equals(GetString(item, "status"), "claimed", StringComparison.OrdinalIgnoreCase)
            && string.Equals(GetString(item, "location"), "yemekhane", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PatchStatus_OpenClaimedClosed_HistoryAndInvalidIs400()
    {
        using var client = ApiClient.NewClient();
        var id = await CreateItemAsync(client, new
        {
            title = $"{Marker}-status",
            description = "status cycle",
            location = "spor salonu",
            category = "kıyafet",
            contact = "status@example.com",
            photoUrl = "https://example.com/status.png",
            kind = "lost",
            status = "open"
        });

        await PatchStatusAsync(client, id, "claimed", HttpStatusCode.OK);
        await PatchStatusAsync(client, id, "closed", HttpStatusCode.OK);

        using var get = await client.GetAsync($"api/items/{id}");
        var body = await get.Content.ReadAsStringAsync();
        Assert.True(get.StatusCode == HttpStatusCode.OK, $"GET after status cycle failed: {ApiClient.Excerpt(body)}");
        var item = JsonSerializer.Deserialize<JsonElement>(body, ApiClient.JsonOptions);
        Assert.Equal("closed", GetString(item, "status"));
        Assert.Equal("status@example.com", GetString(item, "contact"));
        AssertStatusHistory(item, "open", "claimed", "closed");

        using var invalid = await client.PatchAsync($"api/items/{id}/status", ApiClient.JsonBody(new { status = "not-a-status" }));
        var invalidBody = await invalid.Content.ReadAsStringAsync();
        Assert.True(invalid.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 400 for invalid status, got {(int)invalid.StatusCode}: {ApiClient.Excerpt(invalidBody)}");
    }

    [Fact]
    public async Task Contact_HiddenWhileOpen_VisibleWhenClaimedOrClosed()
    {
        using var client = ApiClient.NewClient();
        var payload = new
        {
            title = $"{Marker}-privacy",
            description = "contact must stay private while open",
            location = "merkez",
            category = "diğer",
            contact = "secret@aksaray.edu.tr",
            photoUrl = "https://example.com/privacy.png",
            kind = "found",
            status = "open"
        };

        var id = await CreateItemAsync(client, payload);

        using (var getOpen = await client.GetAsync($"api/items/{id}"))
        {
            var openBody = await getOpen.Content.ReadAsStringAsync();
            var openItem = JsonSerializer.Deserialize<JsonElement>(openBody, ApiClient.JsonOptions);
            AssertContactHidden(openItem);
        }

        using (var list = await client.GetAsync($"api/items?q={Uri.EscapeDataString(payload.title)}"))
        {
            var listBody = await list.Content.ReadAsStringAsync();
            var items = JsonSerializer.Deserialize<List<JsonElement>>(listBody, ApiClient.JsonOptions);
            var listed = Assert.Single(items!, i => i.GetProperty("id").GetInt32() == id);
            AssertContactHidden(listed);
        }

        await PatchStatusAsync(client, id, "claimed", HttpStatusCode.OK);
        using (var getClaimed = await client.GetAsync($"api/items/{id}"))
        {
            var claimedBody = await getClaimed.Content.ReadAsStringAsync();
            var claimed = JsonSerializer.Deserialize<JsonElement>(claimedBody, ApiClient.JsonOptions);
            Assert.Equal("secret@aksaray.edu.tr", GetString(claimed, "contact"));
        }

        await PatchStatusAsync(client, id, "closed", HttpStatusCode.OK);
        using (var getClosed = await client.GetAsync($"api/items/{id}"))
        {
            var closedBody = await getClosed.Content.ReadAsStringAsync();
            var closed = JsonSerializer.Deserialize<JsonElement>(closedBody, ApiClient.JsonOptions);
            Assert.Equal("secret@aksaray.edu.tr", GetString(closed, "contact"));
        }
    }

    [Fact]
    public async Task Matches_OppositeKindSameCategoryLocation_OpenOnly()
    {
        using var client = ApiClient.NewClient();
        var lostOpen = await CreateItemAsync(client, new
        {
            title = $"{Marker}-match-lost",
            description = "source lost phone",
            location = "kütüphane",
            category = "telefon",
            contact = "lost@aksaray.edu.tr",
            photoUrl = "https://example.com/m1.png",
            kind = "lost",
            status = "open"
        });

        var foundOpen = await CreateItemAsync(client, new
        {
            title = $"{Marker}-match-found",
            description = "matching found phone",
            location = "kütüphane",
            category = "telefon",
            contact = "found@aksaray.edu.tr",
            photoUrl = "https://example.com/m2.png",
            kind = "found",
            status = "open"
        });

        var otherLocation = await CreateItemAsync(client, new
        {
            title = $"{Marker}-match-other-loc",
            description = "same category different location",
            location = "yurt",
            category = "telefon",
            contact = "otherloc@aksaray.edu.tr",
            photoUrl = "https://example.com/m3.png",
            kind = "found",
            status = "open"
        });

        var otherCategory = await CreateItemAsync(client, new
        {
            title = $"{Marker}-match-other-cat",
            description = "same location different category",
            location = "kütüphane",
            category = "çanta",
            contact = "othercat@aksaray.edu.tr",
            photoUrl = "https://example.com/m4.png",
            kind = "found",
            status = "open"
        });

        var foundClaimed = await CreateItemAsync(client, new
        {
            title = $"{Marker}-match-claimed",
            description = "would match but claimed",
            location = "kütüphane",
            category = "telefon",
            contact = "claimed@aksaray.edu.tr",
            photoUrl = "https://example.com/m5.png",
            kind = "found",
            status = "claimed"
        });

        var sameKind = await CreateItemAsync(client, new
        {
            title = $"{Marker}-match-same-kind",
            description = "same kind should not match",
            location = "kütüphane",
            category = "telefon",
            contact = "samekind@aksaray.edu.tr",
            photoUrl = "https://example.com/m6.png",
            kind = "lost",
            status = "open"
        });

        using (var response = await client.GetAsync($"api/items/{lostOpen}/matches"))
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK,
                $"Expected 200 from matches, got {(int)response.StatusCode}: {ApiClient.Excerpt(body)}");
            var matches = JsonSerializer.Deserialize<List<JsonElement>>(body, ApiClient.JsonOptions);
            Assert.NotNull(matches);
            Assert.Contains(matches!, i => i.GetProperty("id").GetInt32() == foundOpen);
            Assert.DoesNotContain(matches!, i => i.GetProperty("id").GetInt32() == lostOpen);
            Assert.DoesNotContain(matches!, i => i.GetProperty("id").GetInt32() == otherLocation);
            Assert.DoesNotContain(matches!, i => i.GetProperty("id").GetInt32() == otherCategory);
            Assert.DoesNotContain(matches!, i => i.GetProperty("id").GetInt32() == foundClaimed);
            Assert.DoesNotContain(matches!, i => i.GetProperty("id").GetInt32() == sameKind);
            var match = Assert.Single(matches!, i => i.GetProperty("id").GetInt32() == foundOpen);
            AssertContactHidden(match);
        }

        using (var missing = await client.GetAsync("api/items/999999999/matches"))
        {
            var body = await missing.Content.ReadAsStringAsync();
            Assert.True(missing.StatusCode == HttpStatusCode.NotFound,
                $"Expected 404 for missing source id, got {(int)missing.StatusCode}: {ApiClient.Excerpt(body)}");
        }
    }

    [Fact]
    public async Task Persistence_PostThenGetWithNewClient_SurvivesApiRestart()
    {
        int id;
        var payload = new
        {
            title = $"{Marker}-persist",
            description = "must live in postgres",
            location = "mühendislik",
            category = "öğrenci kartı",
            contact = "persist@example.com",
            photoUrl = "https://example.com/persist.png",
            kind = "found",
            status = "open"
        };

        using (var postClient = ApiClient.NewClient())
        {
            id = await CreateItemAsync(postClient, payload);
        }

        using (var getClient = ApiClient.NewClient())
        {
            using var get = await getClient.GetAsync($"api/items/{id}");
            var body = await get.Content.ReadAsStringAsync();
            Assert.True(get.StatusCode == HttpStatusCode.OK,
                $"Expected persisted item via new HTTP client, got {(int)get.StatusCode}: {ApiClient.Excerpt(body)}");
            AssertFieldsMatch(JsonSerializer.Deserialize<JsonElement>(body, ApiClient.JsonOptions), payload);
        }

        await ApiProcess.RestartAsync();

        using (var afterRestart = ApiClient.NewClient())
        {
            using var get = await afterRestart.GetAsync($"api/items/{id}");
            var body = await get.Content.ReadAsStringAsync();
            Assert.True(get.StatusCode == HttpStatusCode.OK,
                $"Expected item {id} after API restart (PostgreSQL), got {(int)get.StatusCode}: {ApiClient.Excerpt(body)}");
            var item = JsonSerializer.Deserialize<JsonElement>(body, ApiClient.JsonOptions);
            AssertFieldsMatch(item, payload);
            AssertStatusHistory(item, "open");
        }
    }

    [Fact]
    public async Task Negatives_MissingTitle400_UnknownLists400_UnknownGet404_UnknownPatch404()
    {
        using var client = ApiClient.NewClient();

        using (var post = await client.PostAsync("api/items", ApiClient.JsonBody(new { kind = "lost" })))
        {
            var body = await post.Content.ReadAsStringAsync();
            Assert.True(post.StatusCode == HttpStatusCode.BadRequest,
                $"Expected 400 for POST missing title, got {(int)post.StatusCode}: {ApiClient.Excerpt(body)}");
        }

        using (var badKind = await client.PostAsync("api/items", ApiClient.JsonBody(new
        {
            title = $"{Marker}-bad-kind",
            location = "merkez",
            category = "diğer",
            kind = "maybe"
        })))
        {
            var body = await badKind.Content.ReadAsStringAsync();
            Assert.True(badKind.StatusCode == HttpStatusCode.BadRequest,
                $"Expected 400 for invalid kind, got {(int)badKind.StatusCode}: {ApiClient.Excerpt(body)}");
        }

        using (var badLocation = await client.PostAsync("api/items", ApiClient.JsonBody(new
        {
            title = $"{Marker}-bad-location",
            location = "Rektörlük",
            category = "telefon",
            kind = "lost"
        })))
        {
            var body = await badLocation.Content.ReadAsStringAsync();
            Assert.True(badLocation.StatusCode == HttpStatusCode.BadRequest,
                $"Expected 400 for unknown location, got {(int)badLocation.StatusCode}: {ApiClient.Excerpt(body)}");
        }

        using (var badCategory = await client.PostAsync("api/items", ApiClient.JsonBody(new
        {
            title = $"{Marker}-bad-category",
            location = "merkez",
            category = "giyim",
            kind = "lost"
        })))
        {
            var body = await badCategory.Content.ReadAsStringAsync();
            Assert.True(badCategory.StatusCode == HttpStatusCode.BadRequest,
                $"Expected 400 for unknown category, got {(int)badCategory.StatusCode}: {ApiClient.Excerpt(body)}");
        }

        const int unknownId = 999999999;
        using (var get = await client.GetAsync($"api/items/{unknownId}"))
        {
            var body = await get.Content.ReadAsStringAsync();
            Assert.True(get.StatusCode == HttpStatusCode.NotFound,
                $"Expected 404 for GET unknown id, got {(int)get.StatusCode}: {ApiClient.Excerpt(body)}");
        }

        using (var patch = await client.PatchAsync($"api/items/{unknownId}/status", ApiClient.JsonBody(new { status = "claimed" })))
        {
            var body = await patch.Content.ReadAsStringAsync();
            Assert.True(patch.StatusCode == HttpStatusCode.NotFound,
                $"Expected 404 for PATCH unknown id, got {(int)patch.StatusCode}: {ApiClient.Excerpt(body)}");
        }
    }

    [Fact]
    public async Task Cors_OriginLocalhost4200_IsAllowed()
    {
        using var client = ApiClient.NewClient();
        const string origin = "http://localhost:4200";

        using var options = new HttpRequestMessage(HttpMethod.Options, "api/items");
        options.Headers.TryAddWithoutValidation("Origin", origin);
        options.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "GET");
        options.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", "content-type");

        using (var preflight = await client.SendAsync(options))
        {
            Assert.True(HasAllowedOrigin(preflight, origin),
                $"OPTIONS preflight missing Access-Control-Allow-Origin: {origin}. Status={(int)preflight.StatusCode}; headers={FormatHeaders(preflight)}");
        }

        using var get = new HttpRequestMessage(HttpMethod.Get, "api/items");
        get.Headers.TryAddWithoutValidation("Origin", origin);
        using (var response = await client.SendAsync(get))
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK,
                $"GET with Origin failed: {(int)response.StatusCode} {ApiClient.Excerpt(body)}");
            Assert.True(HasAllowedOrigin(response, origin),
                $"GET missing Access-Control-Allow-Origin: {origin}. headers={FormatHeaders(response)}");
        }
    }

    private static async Task<int> CreateItemAsync(HttpClient client, object payload)
    {
        using var post = await client.PostAsync("api/items", ApiClient.JsonBody(payload));
        var body = await post.Content.ReadAsStringAsync();
        Assert.True(post.StatusCode == HttpStatusCode.Created,
            $"Expected 201 creating helper item, got {(int)post.StatusCode}: {ApiClient.Excerpt(body)}");
        var posted = JsonSerializer.Deserialize<JsonElement>(body, ApiClient.JsonOptions);
        AssertContactPolicy(posted);
        return ReadId(posted, $"Helper POST missing id: {ApiClient.Excerpt(body)}");
    }

    private static async Task AssertFilterAsync(
        HttpClient client,
        string url,
        int expectedId,
        Func<JsonElement, bool> predicate)
    {
        using var response = await client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 from {url}, got {(int)response.StatusCode}: {ApiClient.Excerpt(body)}");

        var items = JsonSerializer.Deserialize<List<JsonElement>>(body, ApiClient.JsonOptions);
        Assert.NotNull(items);
        Assert.Contains(items!, item => item.GetProperty("id").GetInt32() == expectedId);
        Assert.All(items!, item =>
        {
            Assert.True(predicate(item), $"Filter {url} returned item that does not match: {item}");
            AssertContactPolicy(item);
        });
    }

    private static async Task PatchStatusAsync(HttpClient client, int id, string status, HttpStatusCode expected)
    {
        using var patch = await client.PatchAsync($"api/items/{id}/status", ApiClient.JsonBody(new { status }));
        var body = await patch.Content.ReadAsStringAsync();
        Assert.True(patch.StatusCode == expected,
            $"PATCH status={status} expected {(int)expected}, got {(int)patch.StatusCode}: {ApiClient.Excerpt(body)}");
        if (expected == HttpStatusCode.OK)
        {
            var item = JsonSerializer.Deserialize<JsonElement>(body, ApiClient.JsonOptions);
            Assert.Equal(status, GetString(item, "status"));
            AssertContactPolicy(item);
        }
    }

    private static void AssertFieldsMatch(JsonElement item, object expected)
    {
        var json = JsonSerializer.Serialize(expected, ApiClient.JsonOptions);
        using var doc = JsonDocument.Parse(json);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Name == "contact")
            {
                AssertContactPolicy(item);
                var status = doc.RootElement.TryGetProperty("status", out var statusEl)
                    ? statusEl.GetString()
                    : GetString(item, "status");
                if (status is "claimed" or "closed")
                {
                    Assert.Equal(prop.Value.GetString(), GetString(item, "contact"));
                }
                continue;
            }

            Assert.True(item.TryGetProperty(prop.Name, out var actual), $"Missing field {prop.Name}");
            Assert.Equal(prop.Value.GetString(), actual.ValueKind == JsonValueKind.Null ? null : actual.GetString());
        }
    }

    private static void AssertStatusHistory(JsonElement item, params string[] expectedStatuses)
    {
        Assert.True(item.TryGetProperty("statusHistory", out var history) && history.ValueKind == JsonValueKind.Array,
            $"Missing statusHistory on item: {item}");
        var statuses = history.EnumerateArray()
            .Select(entry =>
            {
                Assert.True(entry.TryGetProperty("status", out var statusEl), $"History entry missing status: {entry}");
                Assert.True(entry.TryGetProperty("timestamp", out var ts) && ts.ValueKind != JsonValueKind.Null,
                    $"History entry missing timestamp: {entry}");
                return statusEl.GetString();
            })
            .ToList();
        Assert.Equal(expectedStatuses, statuses);
    }

    private static void AssertContactPolicy(JsonElement item)
    {
        var status = GetString(item, "status");
        if (status == "open")
        {
            AssertContactHidden(item);
        }
    }

    private static void AssertContactHidden(JsonElement item)
    {
        if (!item.TryGetProperty("contact", out var contact) || contact.ValueKind == JsonValueKind.Undefined)
        {
            return;
        }

        Assert.True(contact.ValueKind is JsonValueKind.Null,
            $"open item must not return contact, got {contact}: {item}");
    }

    private static int ReadId(JsonElement item, string message)
    {
        if (!item.TryGetProperty("id", out var idEl) || !idEl.TryGetInt32(out var id) || id <= 0)
        {
            Assert.Fail(message);
            return 0;
        }

        return id;
    }

    private static string? GetString(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.GetString()
            : null;

    private static bool ContainsIgnoreCase(string? haystack, string needle) =>
        !string.IsNullOrEmpty(haystack) && haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

    private static bool HasAllowedOrigin(HttpResponseMessage response, string origin)
    {
        if (response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values))
        {
            return values.Any(v => v == origin || v == "*");
        }

        return false;
    }

    private static string FormatHeaders(HttpResponseMessage response)
    {
        var sb = new StringBuilder();
        foreach (var header in response.Headers)
        {
            sb.Append(header.Key).Append('=').Append(string.Join(',', header.Value)).Append(';');
        }

        return sb.ToString();
    }
}

[CollectionDefinition("ApiSmoke")]
public class ApiSmokeCollection : ICollectionFixture<ApiReadyFixture>
{
}

public class ApiReadyFixture : IAsyncLifetime
{
    public Task InitializeAsync() => ApiProcess.WaitUntilReadyAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
