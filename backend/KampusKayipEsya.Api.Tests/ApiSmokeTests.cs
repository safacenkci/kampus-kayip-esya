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
    public async Task Seed_GetItems_WithoutFilters_ReturnsCampusAds()
    {
        using var client = ApiClient.NewClient();
        using var response = await client.GetAsync("api/items");
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 from GET /api/items, got {(int)response.StatusCode}: {ApiClient.Excerpt(body)}");

        var items = JsonSerializer.Deserialize<List<JsonElement>>(body, ApiClient.JsonOptions);
        Assert.NotNull(items);
        Assert.True(items!.Count >= 6,
            $"Expected at least 6 seeded campus ads on a fresh DB, got {items.Count}: {ApiClient.Excerpt(body)}");

        Assert.Contains(items, i => string.Equals(GetString(i, "kind"), "lost", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(items, i => string.Equals(GetString(i, "kind"), "found", StringComparison.OrdinalIgnoreCase));
        Assert.All(items, item =>
        {
            var location = GetString(item, "location");
            var category = GetString(item, "category");
            Assert.Contains(location, ExpectedLocations);
            Assert.Contains(category, ExpectedCategories);
            AssertContactPolicy(item);
        });
    }

    [Fact]
    public async Task Catalogs_CategoriesAndLocations_AreFixedLists()
    {
        using var client = ApiClient.NewClient();

        using (var response = await client.GetAsync("api/categories"))
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK,
                $"Expected 200 from GET /api/categories, got {(int)response.StatusCode}: {ApiClient.Excerpt(body)}");
            var categories = JsonSerializer.Deserialize<List<string>>(body, ApiClient.JsonOptions);
            Assert.Equal(ExpectedCategories, categories);
        }

        using (var response = await client.GetAsync("api/locations"))
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK,
                $"Expected 200 from GET /api/locations, got {(int)response.StatusCode}: {ApiClient.Excerpt(body)}");
            var locations = JsonSerializer.Deserialize<List<string>>(body, ApiClient.JsonOptions);
            Assert.Equal(ExpectedLocations, locations);
        }
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
            contact = "crud@aksaray.edu.tr",
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
            Assert.True(posted.TryGetProperty("statusHistory", out var postHistory) && postHistory.ValueKind == JsonValueKind.Array);

            using (var get = await client.GetAsync($"api/items/{id}"))
            {
                var getBody = await get.Content.ReadAsStringAsync();
                Assert.True(get.StatusCode == HttpStatusCode.OK,
                    $"Expected 200 from GET /api/items/{id}, got {(int)get.StatusCode}: {ApiClient.Excerpt(getBody)}");
                var got = JsonSerializer.Deserialize<JsonElement>(getBody, ApiClient.JsonOptions);
                AssertFieldsMatch(got, created);
                Assert.True(got.TryGetProperty("statusHistory", out var history) && history.ValueKind == JsonValueKind.Array,
                    $"GET item must include statusHistory: {ApiClient.Excerpt(getBody)}");
            }

            var updated = new
            {
                title = $"{Marker}-crud-updated",
                description = "Updated description",
                location = "mühendislik",
                category = "telefon",
                contact = "updated@aksaray.edu.tr",
                photoUrl = "https://example.com/updated.png",
                kind = "found",
                status = "claimed"
            };

            using (var put = await client.PutAsync($"api/items/{id}", ApiClient.JsonBody(updated)))
            {
                var putBody = await put.Content.ReadAsStringAsync();
                Assert.True(put.StatusCode == HttpStatusCode.OK,
                    $"Expected 200 from PUT /api/items/{id}, got {(int)put.StatusCode}: {ApiClient.Excerpt(putBody)}");
                AssertFieldsMatch(JsonSerializer.Deserialize<JsonElement>(putBody, ApiClient.JsonOptions), updated);
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
    public async Task Filters_Q_Kind_Category_Location_Status()
    {
        using var client = ApiClient.NewClient();
        var uniqueQ = $"{Marker}-needle-q";

        var lostOpen = await CreateItemAsync(client, new
        {
            title = $"{Marker}-lost-open",
            description = uniqueQ,
            location = "kütüphane",
            category = "öğrenci kartı",
            contact = "filter@aksaray.edu.tr",
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
            contact = "filter2@aksaray.edu.tr",
            photoUrl = "https://example.com/f2.png",
            kind = "found",
            status = "claimed"
        });

        await AssertFilterAsync(client, $"api/items?q={Uri.EscapeDataString(uniqueQ)}", lostOpen, item =>
            ContainsIgnoreCase(GetString(item, "title"), uniqueQ)
            || ContainsIgnoreCase(GetString(item, "description"), uniqueQ)
            || ContainsIgnoreCase(GetString(item, "location"), uniqueQ));

        await AssertFilterAsync(client, $"api/items?category={Uri.EscapeDataString("öğrenci kartı")}", lostOpen, item =>
            string.Equals(GetString(item, "category"), "öğrenci kartı", StringComparison.OrdinalIgnoreCase));

        await AssertFilterAsync(client, $"api/items?location={Uri.EscapeDataString("kütüphane")}", lostOpen, item =>
            string.Equals(GetString(item, "location"), "kütüphane", StringComparison.OrdinalIgnoreCase));

        await AssertFilterAsync(client, "api/items?status=claimed", foundClaimed, item =>
            string.Equals(GetString(item, "status"), "claimed", StringComparison.OrdinalIgnoreCase));

        await AssertFilterAsync(client, "api/items?kind=found", foundClaimed, item =>
            string.Equals(GetString(item, "kind"), "found", StringComparison.OrdinalIgnoreCase));

        await AssertFilterAsync(client, "api/items?kind=found&status=claimed&location=yemekhane&category=" + Uri.EscapeDataString("çanta"),
            foundClaimed, item =>
                string.Equals(GetString(item, "kind"), "found", StringComparison.OrdinalIgnoreCase)
                && string.Equals(GetString(item, "status"), "claimed", StringComparison.OrdinalIgnoreCase)
                && string.Equals(GetString(item, "location"), "yemekhane", StringComparison.OrdinalIgnoreCase)
                && string.Equals(GetString(item, "category"), "çanta", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PatchStatus_RecordsHistory_AndInvalidIs400()
    {
        using var client = ApiClient.NewClient();
        var id = await CreateItemAsync(client, new
        {
            title = $"{Marker}-status",
            description = "status cycle",
            location = "spor salonu",
            category = "kıyafet",
            contact = "status@aksaray.edu.tr",
            photoUrl = "https://example.com/status.png",
            kind = "lost",
            status = "open"
        });

        using (var claimed = await client.PatchAsync($"api/items/{id}/status", ApiClient.JsonBody(new { status = "claimed" })))
        {
            var body = await claimed.Content.ReadAsStringAsync();
            Assert.True(claimed.StatusCode == HttpStatusCode.OK,
                $"PATCH claimed expected 200, got {(int)claimed.StatusCode}: {ApiClient.Excerpt(body)}");
            var item = JsonSerializer.Deserialize<JsonElement>(body, ApiClient.JsonOptions);
            Assert.Equal("claimed", GetString(item, "status"));
            Assert.Equal("status@aksaray.edu.tr", GetString(item, "contact"));
            AssertHistoryContains(item, "open", "claimed");
        }

        using (var closed = await client.PatchAsync($"api/items/{id}/status", ApiClient.JsonBody(new { status = "closed" })))
        {
            var body = await closed.Content.ReadAsStringAsync();
            Assert.True(closed.StatusCode == HttpStatusCode.OK, $"PATCH closed failed: {ApiClient.Excerpt(body)}");
            var item = JsonSerializer.Deserialize<JsonElement>(body, ApiClient.JsonOptions);
            Assert.Equal("closed", GetString(item, "status"));
            AssertHistoryContains(item, "open", "claimed");
            AssertHistoryContains(item, "claimed", "closed");
        }

        using var get = await client.GetAsync($"api/items/{id}");
        var getBody = await get.Content.ReadAsStringAsync();
        Assert.True(get.StatusCode == HttpStatusCode.OK, $"GET after status cycle failed: {ApiClient.Excerpt(getBody)}");
        var got = JsonSerializer.Deserialize<JsonElement>(getBody, ApiClient.JsonOptions);
        Assert.Equal("closed", GetString(got, "status"));
        Assert.Equal("status@aksaray.edu.tr", GetString(got, "contact"));
        AssertHistoryContains(got, "claimed", "closed");

        using var invalid = await client.PatchAsync($"api/items/{id}/status", ApiClient.JsonBody(new { status = "not-a-status" }));
        var invalidBody = await invalid.Content.ReadAsStringAsync();
        Assert.True(invalid.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 400 for invalid status, got {(int)invalid.StatusCode}: {ApiClient.Excerpt(invalidBody)}");
    }

    [Fact]
    public async Task Matches_OppositeKind_SameCategoryLocation_OpenOnly()
    {
        using var client = ApiClient.NewClient();
        var location = "merkez";
        var category = "kulaklık";

        var lost = await CreateItemAsync(client, new
        {
            title = $"{Marker}-match-lost",
            description = "lost headphones for match",
            location,
            category,
            contact = "lost-match@aksaray.edu.tr",
            kind = "lost",
            status = "open"
        });

        var foundOpen = await CreateItemAsync(client, new
        {
            title = $"{Marker}-match-found",
            description = "found headphones for match",
            location,
            category,
            contact = "found-match@aksaray.edu.tr",
            kind = "found",
            status = "open"
        });

        var foundOtherLocation = await CreateItemAsync(client, new
        {
            title = $"{Marker}-match-wrong-loc",
            description = "same category other location",
            location = "yurt",
            category,
            contact = "wrong-loc@aksaray.edu.tr",
            kind = "found",
            status = "open"
        });

        var foundClaimed = await CreateItemAsync(client, new
        {
            title = $"{Marker}-match-claimed",
            description = "same place but claimed",
            location,
            category,
            contact = "claimed-match@aksaray.edu.tr",
            kind = "found",
            status = "claimed"
        });

        using var response = await client.GetAsync($"api/items/{lost}/matches");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 from matches, got {(int)response.StatusCode}: {ApiClient.Excerpt(body)}");

        var matches = JsonSerializer.Deserialize<List<JsonElement>>(body, ApiClient.JsonOptions);
        Assert.NotNull(matches);
        var ids = matches!.Select(m => m.GetProperty("id").GetInt32()).ToList();
        Assert.Contains(foundOpen, ids);
        Assert.DoesNotContain(lost, ids);
        Assert.DoesNotContain(foundOtherLocation, ids);
        Assert.DoesNotContain(foundClaimed, ids);
        Assert.All(matches, item =>
        {
            Assert.Equal("found", GetString(item, "kind"));
            Assert.Equal("open", GetString(item, "status"));
            Assert.Equal(location, GetString(item, "location"));
            Assert.Equal(category, GetString(item, "category"));
            AssertContactPolicy(item);
        });

        using var missing = await client.GetAsync("api/items/999999999/matches");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Contact_HiddenWhileOpen_VisibleAfterClaimed()
    {
        using var client = ApiClient.NewClient();
        const string contact = "gizli@aksaray.edu.tr";
        var id = await CreateItemAsync(client, new
        {
            title = $"{Marker}-contact",
            description = "contact hiding",
            location = "yemekhane",
            category = "çanta",
            contact,
            kind = "found",
            status = "open"
        });

        using (var getOpen = await client.GetAsync($"api/items/{id}"))
        {
            var body = await getOpen.Content.ReadAsStringAsync();
            var item = JsonSerializer.Deserialize<JsonElement>(body, ApiClient.JsonOptions);
            Assert.Equal("open", GetString(item, "status"));
            AssertContactHidden(item);
        }

        using (var list = await client.GetAsync($"api/items?q={Uri.EscapeDataString(Marker + "-contact")}"))
        {
            var body = await list.Content.ReadAsStringAsync();
            var items = JsonSerializer.Deserialize<List<JsonElement>>(body, ApiClient.JsonOptions);
            var item = Assert.Single(items!, i => i.GetProperty("id").GetInt32() == id);
            AssertContactHidden(item);
        }

        await PatchStatusAsync(client, id, "claimed", HttpStatusCode.OK);

        using (var getClaimed = await client.GetAsync($"api/items/{id}"))
        {
            var body = await getClaimed.Content.ReadAsStringAsync();
            var item = JsonSerializer.Deserialize<JsonElement>(body, ApiClient.JsonOptions);
            Assert.Equal("claimed", GetString(item, "status"));
            Assert.Equal(contact, GetString(item, "contact"));
        }
    }

    [Fact]
    public async Task InvalidLocationOrCategory_Is400()
    {
        using var client = ApiClient.NewClient();

        using (var postLoc = await client.PostAsync("api/items", ApiClient.JsonBody(new
        {
            title = $"{Marker}-bad-loc",
            location = "Rektörlük",
            category = "anahtar",
            kind = "lost"
        })))
        {
            var body = await postLoc.Content.ReadAsStringAsync();
            Assert.True(postLoc.StatusCode == HttpStatusCode.BadRequest,
                $"Expected 400 for invalid location, got {(int)postLoc.StatusCode}: {ApiClient.Excerpt(body)}");
        }

        using (var postCat = await client.PostAsync("api/items", ApiClient.JsonBody(new
        {
            title = $"{Marker}-bad-cat",
            location = "merkez",
            category = "giyim",
            kind = "found"
        })))
        {
            var body = await postCat.Content.ReadAsStringAsync();
            Assert.True(postCat.StatusCode == HttpStatusCode.BadRequest,
                $"Expected 400 for invalid category, got {(int)postCat.StatusCode}: {ApiClient.Excerpt(body)}");
        }

        using (var filterLoc = await client.GetAsync("api/items?location=Rektörlük"))
        {
            Assert.Equal(HttpStatusCode.BadRequest, filterLoc.StatusCode);
        }

        using (var filterCat = await client.GetAsync("api/items?category=giyim"))
        {
            Assert.Equal(HttpStatusCode.BadRequest, filterCat.StatusCode);
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
            category = "telefon",
            contact = "persist@aksaray.edu.tr",
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
            Assert.True(item.TryGetProperty("statusHistory", out var history) && history.ValueKind == JsonValueKind.Array);
        }
    }

    [Fact]
    public async Task Negatives_MissingTitle400_UnknownGet404_UnknownPatch404()
    {
        using var client = ApiClient.NewClient();

        using (var post = await client.PostAsync("api/items", ApiClient.JsonBody(new { kind = "lost" })))
        {
            var body = await post.Content.ReadAsStringAsync();
            Assert.True(post.StatusCode == HttpStatusCode.BadRequest,
                $"Expected 400 for POST missing title, got {(int)post.StatusCode}: {ApiClient.Excerpt(body)}");
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
        Assert.All(items!, item => Assert.True(predicate(item),
            $"Filter {url} returned item that does not match: {item}"));
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
        }
    }

    private static void AssertFieldsMatch(JsonElement item, object expected)
    {
        var json = JsonSerializer.Serialize(expected, ApiClient.JsonOptions);
        using var doc = JsonDocument.Parse(json);
        var expectedStatus = doc.RootElement.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : GetString(item, "status");

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Name == "contact" && string.Equals(expectedStatus, "open", StringComparison.OrdinalIgnoreCase))
            {
                AssertContactHidden(item);
                continue;
            }

            Assert.True(item.TryGetProperty(prop.Name, out var actual), $"Missing field {prop.Name}");
            Assert.Equal(prop.Value.GetString(), actual.ValueKind == JsonValueKind.Null ? null : actual.GetString());
        }

        AssertContactPolicy(item);
    }

    private static void AssertContactPolicy(JsonElement item)
    {
        var status = GetString(item, "status");
        if (string.Equals(status, "open", StringComparison.OrdinalIgnoreCase))
        {
            AssertContactHidden(item);
        }
    }

    private static void AssertContactHidden(JsonElement item)
    {
        if (!item.TryGetProperty("contact", out var contact))
        {
            return;
        }

        Assert.True(contact.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined,
            $"open item must omit contact, got {contact}");
    }

    private static void AssertHistoryContains(JsonElement item, string from, string to)
    {
        Assert.True(item.TryGetProperty("statusHistory", out var history) && history.ValueKind == JsonValueKind.Array,
            "Expected statusHistory array");
        Assert.Contains(history.EnumerateArray(), entry =>
            string.Equals(GetString(entry, "from"), from, StringComparison.OrdinalIgnoreCase)
            && string.Equals(GetString(entry, "to"), to, StringComparison.OrdinalIgnoreCase)
            && GetString(entry, "changedAt") is not null);
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
