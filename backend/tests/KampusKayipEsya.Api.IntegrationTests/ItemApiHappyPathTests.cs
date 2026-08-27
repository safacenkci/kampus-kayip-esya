using System.Net;
using System.Text.Json;
using KampusKayipEsya.Api.IntegrationTests.Infrastructure;

namespace KampusKayipEsya.Api.IntegrationTests;

public sealed class ItemApiHappyPathTests : IntegrationTestBase
{
    private static readonly string Marker = $"it-{Guid.NewGuid():N}";

    private static readonly string[] ExpectedLocations =
    [
        "merkez", "kütüphane", "yemekhane", "mühendislik", "yurt", "spor salonu"
    ];

    private static readonly string[] ExpectedCategories =
    [
        "öğrenci kartı", "anahtar", "telefon", "çanta", "kıyafet", "kulaklık", "diğer"
    ];

    public ItemApiHappyPathTests(PostgresApiFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task GetItems_ReturnsSeededCampusAds()
    {
        using var response = await Client.GetAsync("api/items");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 from GET /api/items, got {(int)response.StatusCode}: {Excerpt(body)}");

        var items = JsonSerializer.Deserialize<List<JsonElement>>(body, JsonOptions);
        Assert.NotNull(items);
        Assert.True(items!.Count >= 6, $"Expected at least 6 seeded ads, got {items.Count}");
        Assert.Contains(items, i => string.Equals(ReadString(i, "kind"), "lost", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(items, i => string.Equals(ReadString(i, "kind"), "found", StringComparison.OrdinalIgnoreCase));
        Assert.All(items, item =>
        {
            Assert.Contains(ReadString(item, "location"), ExpectedLocations);
            Assert.Contains(ReadString(item, "category"), ExpectedCategories);
            AssertNoContact(item);
            AssertNoManageToken(item);
        });
        AssertListHasNoContact(body);
    }

    [Fact]
    public async Task GetItems_FiltersByQ_Kind_Category_Location_Status()
    {
        var uniqueQ = $"{Marker}-needle-q";
        var (lostOpen, _, _) = await CreateItemAsync(Client, new
        {
            title = $"{Marker}-lost-open",
            description = uniqueQ,
            location = "kütüphane",
            category = "öğrenci kartı",
            contact = "filter@aksaray.edu.tr",
            kind = "lost",
            status = "open"
        });
        var (foundClaimed, claimedToken, _) = await CreateItemAsync(Client, new
        {
            title = $"{Marker}-found-claimed",
            description = "other description",
            location = "yemekhane",
            category = "çanta",
            contact = "filter2@aksaray.edu.tr",
            kind = "found",
            status = "claimed"
        });
        using (await SendAsync(Client, HttpMethod.Patch, $"api/items/{foundClaimed}/status", new { status = "claimed" }, claimedToken))
        {
        }

        await AssertFilterAsync($"api/items?q={Uri.EscapeDataString(uniqueQ)}", lostOpen, item =>
            ContainsIgnoreCase(ReadString(item, "title"), uniqueQ)
            || ContainsIgnoreCase(ReadString(item, "description"), uniqueQ)
            || ContainsIgnoreCase(ReadString(item, "location"), uniqueQ));

        await AssertFilterAsync($"api/items?category={Uri.EscapeDataString("öğrenci kartı")}", lostOpen, item =>
            string.Equals(ReadString(item, "category"), "öğrenci kartı", StringComparison.OrdinalIgnoreCase));

        await AssertFilterAsync($"api/items?location={Uri.EscapeDataString("kütüphane")}", lostOpen, item =>
            string.Equals(ReadString(item, "location"), "kütüphane", StringComparison.OrdinalIgnoreCase));

        await AssertFilterAsync("api/items?status=claimed", foundClaimed, item =>
            string.Equals(ReadString(item, "status"), "claimed", StringComparison.OrdinalIgnoreCase));

        await AssertFilterAsync("api/items?kind=found", foundClaimed, item =>
            string.Equals(ReadString(item, "kind"), "found", StringComparison.OrdinalIgnoreCase));

        await AssertFilterAsync(
            "api/items?kind=found&status=claimed&location=yemekhane&category=" + Uri.EscapeDataString("çanta"),
            foundClaimed,
            item =>
                string.Equals(ReadString(item, "kind"), "found", StringComparison.OrdinalIgnoreCase)
                && string.Equals(ReadString(item, "status"), "claimed", StringComparison.OrdinalIgnoreCase)
                && string.Equals(ReadString(item, "location"), "yemekhane", StringComparison.OrdinalIgnoreCase)
                && string.Equals(ReadString(item, "category"), "çanta", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetItem_IncludesStatusHistory_AndHidesContactWithoutToken()
    {
        var created = new
        {
            title = $"{Marker}-detail",
            description = "detail description",
            location = "yurt",
            category = "anahtar",
            contact = "detail@aksaray.edu.tr",
            photoUrl = "https://example.com/detail.png",
            kind = "lost",
            status = "open"
        };
        var (id, token, _) = await CreateItemAsync(Client, created);

        using (var get = await Client.GetAsync($"api/items/{id}"))
        {
            var text = await get.Content.ReadAsStringAsync();
            Assert.True(get.StatusCode == HttpStatusCode.OK, $"GET item failed: {Excerpt(text)}");
            var item = JsonSerializer.Deserialize<JsonElement>(text, JsonOptions);
            Assert.Equal(created.title, ReadString(item, "title"));
            Assert.Equal(created.location, ReadString(item, "location"));
            Assert.True(item.TryGetProperty("statusHistory", out var history) && history.ValueKind == JsonValueKind.Array);
            AssertNoContact(item);
            AssertNoManageToken(item);
        }

        using (var owned = await SendAsync(Client, HttpMethod.Get, $"api/items/{id}", manageToken: token))
        {
            var text = await owned.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, owned.StatusCode);
            var item = JsonSerializer.Deserialize<JsonElement>(text, JsonOptions);
            Assert.Equal(created.contact, ReadString(item, "contact"));
        }
    }

    [Fact]
    public async Task GetMatches_OppositeKind_SameCategoryLocation_OpenOnly()
    {
        const string location = "merkez";
        const string category = "kulaklık";

        var (lost, _, _) = await CreateItemAsync(Client, new
        {
            title = $"{Marker}-match-lost",
            location,
            category,
            kind = "lost",
            status = "open"
        });
        var (foundOpen, _, _) = await CreateItemAsync(Client, new
        {
            title = $"{Marker}-match-found",
            location,
            category,
            kind = "found",
            status = "open"
        });
        var (wrongLocation, _, _) = await CreateItemAsync(Client, new
        {
            title = $"{Marker}-match-wrong-loc",
            location = "yurt",
            category,
            kind = "found",
            status = "open"
        });
        var (foundClaimed, claimedToken, _) = await CreateItemAsync(Client, new
        {
            title = $"{Marker}-match-claimed",
            location,
            category,
            kind = "found",
            status = "open"
        });
        using (var patch = await SendAsync(Client, HttpMethod.Patch, $"api/items/{foundClaimed}/status", new { status = "claimed" }, claimedToken))
        {
            Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        }

        using var response = await Client.GetAsync($"api/items/{lost}/matches");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"matches failed: {Excerpt(body)}");

        var matches = JsonSerializer.Deserialize<List<JsonElement>>(body, JsonOptions);
        Assert.NotNull(matches);
        var ids = matches!.Select(m => m.GetProperty("id").GetInt32()).ToList();
        Assert.Contains(foundOpen, ids);
        Assert.DoesNotContain(lost, ids);
        Assert.DoesNotContain(wrongLocation, ids);
        Assert.DoesNotContain(foundClaimed, ids);
        Assert.All(matches, item =>
        {
            Assert.Equal("found", ReadString(item, "kind"));
            Assert.Equal("open", ReadString(item, "status"));
            Assert.Equal(location, ReadString(item, "location"));
            Assert.Equal(category, ReadString(item, "category"));
            AssertNoContact(item);
        });
        AssertListHasNoContact(body);
    }

    [Fact]
    public async Task PostPutPatchDelete_RoundTripWithManageToken()
    {
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
        var (id, token, posted) = await CreateItemAsync(Client, created);
        Assert.Equal(created.title, ReadString(posted, "title"));
        Assert.True(posted.TryGetProperty("statusHistory", out var postHistory) && postHistory.ValueKind == JsonValueKind.Array);

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
        using (var put = await SendAsync(Client, HttpMethod.Put, $"api/items/{id}", updated, token))
        {
            var putBody = await put.Content.ReadAsStringAsync();
            Assert.True(put.StatusCode == HttpStatusCode.OK, $"PUT failed: {Excerpt(putBody)}");
            var item = JsonSerializer.Deserialize<JsonElement>(putBody, JsonOptions);
            Assert.Equal(updated.title, ReadString(item, "title"));
            Assert.Equal(updated.location, ReadString(item, "location"));
            Assert.Equal("claimed", ReadString(item, "status"));
            AssertNoContact(item);
            AssertNoManageToken(item);
            AssertHistoryContains(item, "open", "claimed");
        }

        using (var patch = await SendAsync(Client, HttpMethod.Patch, $"api/items/{id}/status", new { status = "closed" }, token))
        {
            var patchBody = await patch.Content.ReadAsStringAsync();
            Assert.True(patch.StatusCode == HttpStatusCode.OK, $"PATCH failed: {Excerpt(patchBody)}");
            var item = JsonSerializer.Deserialize<JsonElement>(patchBody, JsonOptions);
            Assert.Equal("closed", ReadString(item, "status"));
            AssertHistoryContains(item, "claimed", "closed");
            AssertNoContact(item);
        }

        using (var del = await SendAsync(Client, HttpMethod.Delete, $"api/items/{id}", manageToken: token))
        {
            var delBody = await del.Content.ReadAsStringAsync();
            Assert.True(del.StatusCode == HttpStatusCode.NoContent,
                $"Expected 204 from DELETE, got {(int)del.StatusCode}: {Excerpt(delBody)}");
        }

        using var gone = await Client.GetAsync($"api/items/{id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    private async Task AssertFilterAsync(string url, int expectedId, Func<JsonElement, bool> predicate)
    {
        using var response = await Client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 from {url}, got {(int)response.StatusCode}: {Excerpt(body)}");

        var items = JsonSerializer.Deserialize<List<JsonElement>>(body, JsonOptions);
        Assert.NotNull(items);
        Assert.Contains(items!, item => item.GetProperty("id").GetInt32() == expectedId);
        Assert.All(items!, item =>
        {
            Assert.True(predicate(item), $"Filter {url} returned a non-matching item: {item}");
            AssertNoContact(item);
        });
        AssertListHasNoContact(body);
    }

    private static bool ContainsIgnoreCase(string? haystack, string needle) =>
        !string.IsNullOrEmpty(haystack) && haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
