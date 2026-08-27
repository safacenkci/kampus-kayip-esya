using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace KampusKayipEsya.Api.Tests;

[Collection("ApiSmoke")]
public class ApiSmokeTests
{
    private static readonly string Marker = $"smoke-{Guid.NewGuid():N}";

    [Fact]
    public async Task Seed_GetItems_WithoutFilters_ReturnsAtLeastFive()
    {
        using var client = ApiClient.NewClient();
        using var response = await client.GetAsync("api/items");
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 from GET /api/items, got {(int)response.StatusCode}: {ApiClient.Excerpt(body)}");

        var items = JsonSerializer.Deserialize<List<JsonElement>>(body, ApiClient.JsonOptions);
        Assert.NotNull(items);
        Assert.True(items!.Count >= 5,
            $"Expected at least 5 seeded items on a fresh DB, got {items.Count}: {ApiClient.Excerpt(body)}");
    }

    [Fact]
    public async Task Categories_Get_ReturnsUsableList()
    {
        using var client = ApiClient.NewClient();
        using var response = await client.GetAsync("api/categories");
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 from GET /api/categories, got {(int)response.StatusCode}: {ApiClient.Excerpt(body)}");

        var categories = JsonSerializer.Deserialize<List<string>>(body, ApiClient.JsonOptions);
        Assert.NotNull(categories);
        Assert.True(categories!.Count > 0, $"Expected a non-empty category list: {ApiClient.Excerpt(body)}");
        Assert.Contains(categories, c => string.Equals(c, "giyim", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(categories, c => string.Equals(c, "elektronik", StringComparison.OrdinalIgnoreCase));
        Assert.All(categories, c => Assert.False(string.IsNullOrWhiteSpace(c)));
    }

    [Fact]
    public async Task Crud_PostGetPutDelete_RoundTrip()
    {
        using var client = ApiClient.NewClient();
        var created = new
        {
            title = $"{Marker}-crud-title",
            description = "CRUD description",
            location = "A Blok",
            category = "kitap",
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

            using (var get = await client.GetAsync($"api/items/{id}"))
            {
                var getBody = await get.Content.ReadAsStringAsync();
                Assert.True(get.StatusCode == HttpStatusCode.OK,
                    $"Expected 200 from GET /api/items/{id}, got {(int)get.StatusCode}: {ApiClient.Excerpt(getBody)}");
                AssertFieldsMatch(JsonSerializer.Deserialize<JsonElement>(getBody, ApiClient.JsonOptions), created);
            }

            var updated = new
            {
                title = $"{Marker}-crud-updated",
                description = "Updated description",
                location = "B Blok",
                category = "elektronik",
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
    public async Task Filters_Q_Category_Status_Kind_AndCombined()
    {
        using var client = ApiClient.NewClient();
        var uniqueQ = $"{Marker}-needle-q";
        var uniqueCategory = $"{Marker}-cat";

        var lostOpen = await CreateItemAsync(client, new
        {
            title = $"{Marker}-lost-open",
            description = uniqueQ,
            location = "Kütüphane",
            category = uniqueCategory,
            contact = "filter@example.com",
            photoUrl = "https://example.com/f1.png",
            kind = "lost",
            status = "open"
        });

        var foundClaimed = await CreateItemAsync(client, new
        {
            title = $"{Marker}-found-claimed",
            description = "other description",
            location = "Yemekhane",
            category = "aksesuar",
            contact = "filter2@example.com",
            photoUrl = "https://example.com/f2.png",
            kind = "found",
            status = "claimed"
        });

        await AssertFilterAsync(client, $"api/items?q={Uri.EscapeDataString(uniqueQ)}", lostOpen, item =>
            ContainsIgnoreCase(GetString(item, "title"), uniqueQ)
            || ContainsIgnoreCase(GetString(item, "description"), uniqueQ)
            || ContainsIgnoreCase(GetString(item, "location"), uniqueQ));

        await AssertFilterAsync(client, $"api/items?category={Uri.EscapeDataString(uniqueCategory)}", lostOpen, item =>
            string.Equals(GetString(item, "category"), uniqueCategory, StringComparison.OrdinalIgnoreCase));

        await AssertFilterAsync(client, "api/items?status=claimed", foundClaimed, item =>
            string.Equals(GetString(item, "status"), "claimed", StringComparison.OrdinalIgnoreCase));

        await AssertFilterAsync(client, "api/items?kind=found", foundClaimed, item =>
            string.Equals(GetString(item, "kind"), "found", StringComparison.OrdinalIgnoreCase));

        await AssertFilterAsync(client, "api/items?kind=found&status=claimed", foundClaimed, item =>
            string.Equals(GetString(item, "kind"), "found", StringComparison.OrdinalIgnoreCase)
            && string.Equals(GetString(item, "status"), "claimed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PatchStatus_OpenClaimedClosed_AndInvalidIs400()
    {
        using var client = ApiClient.NewClient();
        var id = await CreateItemAsync(client, new
        {
            title = $"{Marker}-status",
            description = "status cycle",
            location = "Spor Salonu",
            category = "giyim",
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

        using var invalid = await client.PatchAsync($"api/items/{id}/status", ApiClient.JsonBody(new { status = "not-a-status" }));
        var invalidBody = await invalid.Content.ReadAsStringAsync();
        Assert.True(invalid.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 400 for invalid status, got {(int)invalid.StatusCode}: {ApiClient.Excerpt(invalidBody)}");
    }

    [Fact]
    public async Task Persistence_PostThenGetWithNewClient_SurvivesApiRestart()
    {
        int id;
        var payload = new
        {
            title = $"{Marker}-persist",
            description = "must live in postgres",
            location = "Rektörlük",
            category = "belgeler",
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
            AssertFieldsMatch(JsonSerializer.Deserialize<JsonElement>(body, ApiClient.JsonOptions), payload);
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
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            Assert.True(item.TryGetProperty(prop.Name, out var actual), $"Missing field {prop.Name}");
            Assert.Equal(prop.Value.GetString(), actual.ValueKind == JsonValueKind.Null ? null : actual.GetString());
        }
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
