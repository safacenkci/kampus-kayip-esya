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

    private static readonly string[] SeedContactValues =
    [
        "elif.demir@aksaray.edu.tr",
        "kutuphane@aksaray.edu.tr",
        "zeynep.kaya@aksaray.edu.tr",
        "muhendislik.guvenlik@aksaray.edu.tr",
        "mehmet.yilmaz@aksaray.edu.tr",
        "spor@aksaray.edu.tr",
        "ayse.ozturk@aksaray.edu.tr",
        "yemekhane@aksaray.edu.tr"
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
        Assert.Contains(items, i => string.Equals(GetString(i, "status"), "claimed", StringComparison.OrdinalIgnoreCase));
        Assert.All(items, item =>
        {
            var location = GetString(item, "location");
            var category = GetString(item, "category");
            Assert.Contains(location, ExpectedLocations);
            Assert.Contains(category, ExpectedCategories);
            AssertContactAbsent(item);
            AssertManageTokenAbsent(item);
        });
        AssertListBodyHasNoContact(body);
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
            var token = ReadManageToken(posted, $"POST 201 body missing manageToken: {ApiClient.Excerpt(postBody)}");
            AssertFieldsMatch(posted, created);
            AssertContactAbsent(posted);
            Assert.True(posted.TryGetProperty("statusHistory", out var postHistory) && postHistory.ValueKind == JsonValueKind.Array);

            using (var get = await client.GetAsync($"api/items/{id}"))
            {
                var getBody = await get.Content.ReadAsStringAsync();
                Assert.True(get.StatusCode == HttpStatusCode.OK,
                    $"Expected 200 from GET /api/items/{id}, got {(int)get.StatusCode}: {ApiClient.Excerpt(getBody)}");
                var got = JsonSerializer.Deserialize<JsonElement>(getBody, ApiClient.JsonOptions);
                AssertFieldsMatch(got, created);
                AssertContactAbsent(got);
                AssertManageTokenAbsent(got);
                Assert.True(got.TryGetProperty("statusHistory", out var history) && history.ValueKind == JsonValueKind.Array,
                    $"GET item must include statusHistory: {ApiClient.Excerpt(getBody)}");
            }

            using (var getOwned = await SendAsync(client, HttpMethod.Get, $"api/items/{id}", manageToken: token))
            {
                var getBody = await getOwned.Content.ReadAsStringAsync();
                Assert.True(getOwned.StatusCode == HttpStatusCode.OK,
                    $"Expected 200 from GET with manage token, got {(int)getOwned.StatusCode}: {ApiClient.Excerpt(getBody)}");
                var got = JsonSerializer.Deserialize<JsonElement>(getBody, ApiClient.JsonOptions);
                Assert.Equal(created.contact, GetString(got, "contact"));
                AssertManageTokenAbsent(got);
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

            using (var put = await SendAsync(client, HttpMethod.Put, $"api/items/{id}", updated, token))
            {
                var putBody = await put.Content.ReadAsStringAsync();
                Assert.True(put.StatusCode == HttpStatusCode.OK,
                    $"Expected 200 from PUT /api/items/{id}, got {(int)put.StatusCode}: {ApiClient.Excerpt(putBody)}");
                var putItem = JsonSerializer.Deserialize<JsonElement>(putBody, ApiClient.JsonOptions);
                AssertFieldsMatch(putItem, updated);
                AssertContactAbsent(putItem);
                AssertManageTokenAbsent(putItem);
            }

            using (var getUpdated = await SendAsync(client, HttpMethod.Get, $"api/items/{id}", manageToken: token))
            {
                var getBody = await getUpdated.Content.ReadAsStringAsync();
                var got = JsonSerializer.Deserialize<JsonElement>(getBody, ApiClient.JsonOptions);
                Assert.Equal(updated.contact, GetString(got, "contact"));
                Assert.Equal("claimed", GetString(got, "status"));
            }

            using (var del = await SendAsync(client, HttpMethod.Delete, $"api/items/{id}", manageToken: token))
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

        await AssertFilterAsync(client, $"api/items?q={Uri.EscapeDataString(uniqueQ)}", lostOpen.Id, item =>
            ContainsIgnoreCase(GetString(item, "title"), uniqueQ)
            || ContainsIgnoreCase(GetString(item, "description"), uniqueQ)
            || ContainsIgnoreCase(GetString(item, "location"), uniqueQ));

        await AssertFilterAsync(client, $"api/items?category={Uri.EscapeDataString("öğrenci kartı")}", lostOpen.Id, item =>
            string.Equals(GetString(item, "category"), "öğrenci kartı", StringComparison.OrdinalIgnoreCase));

        await AssertFilterAsync(client, $"api/items?location={Uri.EscapeDataString("kütüphane")}", lostOpen.Id, item =>
            string.Equals(GetString(item, "location"), "kütüphane", StringComparison.OrdinalIgnoreCase));

        await AssertFilterAsync(client, "api/items?status=claimed", foundClaimed.Id, item =>
            string.Equals(GetString(item, "status"), "claimed", StringComparison.OrdinalIgnoreCase));

        await AssertFilterAsync(client, "api/items?kind=found", foundClaimed.Id, item =>
            string.Equals(GetString(item, "kind"), "found", StringComparison.OrdinalIgnoreCase));

        await AssertFilterAsync(client, "api/items?kind=found&status=claimed&location=yemekhane&category=" + Uri.EscapeDataString("çanta"),
            foundClaimed.Id, item =>
                string.Equals(GetString(item, "kind"), "found", StringComparison.OrdinalIgnoreCase)
                && string.Equals(GetString(item, "status"), "claimed", StringComparison.OrdinalIgnoreCase)
                && string.Equals(GetString(item, "location"), "yemekhane", StringComparison.OrdinalIgnoreCase)
                && string.Equals(GetString(item, "category"), "çanta", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PatchStatus_RecordsHistory_AndInvalidIs400()
    {
        using var client = ApiClient.NewClient();
        var created = await CreateItemAsync(client, new
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
        var id = created.Id;
        var token = created.ManageToken;

        using (var claimed = await SendAsync(client, HttpMethod.Patch, $"api/items/{id}/status", new { status = "claimed" }, token))
        {
            var body = await claimed.Content.ReadAsStringAsync();
            Assert.True(claimed.StatusCode == HttpStatusCode.OK,
                $"PATCH claimed expected 200, got {(int)claimed.StatusCode}: {ApiClient.Excerpt(body)}");
            var item = JsonSerializer.Deserialize<JsonElement>(body, ApiClient.JsonOptions);
            Assert.Equal("claimed", GetString(item, "status"));
            AssertContactAbsent(item);
            AssertHistoryContains(item, "open", "claimed");
        }

        using (var closed = await SendAsync(client, HttpMethod.Patch, $"api/items/{id}/status", new { status = "closed" }, token))
        {
            var body = await closed.Content.ReadAsStringAsync();
            Assert.True(closed.StatusCode == HttpStatusCode.OK, $"PATCH closed failed: {ApiClient.Excerpt(body)}");
            var item = JsonSerializer.Deserialize<JsonElement>(body, ApiClient.JsonOptions);
            Assert.Equal("closed", GetString(item, "status"));
            AssertContactAbsent(item);
            AssertHistoryContains(item, "open", "claimed");
            AssertHistoryContains(item, "claimed", "closed");
        }

        using var get = await client.GetAsync($"api/items/{id}");
        var getBody = await get.Content.ReadAsStringAsync();
        Assert.True(get.StatusCode == HttpStatusCode.OK, $"GET after status cycle failed: {ApiClient.Excerpt(getBody)}");
        var got = JsonSerializer.Deserialize<JsonElement>(getBody, ApiClient.JsonOptions);
        Assert.Equal("closed", GetString(got, "status"));
        AssertContactAbsent(got);
        AssertHistoryContains(got, "claimed", "closed");

        using var getOwned = await SendAsync(client, HttpMethod.Get, $"api/items/{id}", manageToken: token);
        var ownedBody = await getOwned.Content.ReadAsStringAsync();
        var owned = JsonSerializer.Deserialize<JsonElement>(ownedBody, ApiClient.JsonOptions);
        Assert.Equal("status@aksaray.edu.tr", GetString(owned, "contact"));

        using var invalid = await SendAsync(client, HttpMethod.Patch, $"api/items/{id}/status", new { status = "not-a-status" }, token);
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

        using var response = await client.GetAsync($"api/items/{lost.Id}/matches");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 from matches, got {(int)response.StatusCode}: {ApiClient.Excerpt(body)}");

        var matches = JsonSerializer.Deserialize<List<JsonElement>>(body, ApiClient.JsonOptions);
        Assert.NotNull(matches);
        var ids = matches!.Select(m => m.GetProperty("id").GetInt32()).ToList();
        Assert.Contains(foundOpen.Id, ids);
        Assert.DoesNotContain(lost.Id, ids);
        Assert.DoesNotContain(foundOtherLocation.Id, ids);
        Assert.DoesNotContain(foundClaimed.Id, ids);
        Assert.All(matches, item =>
        {
            Assert.Equal("found", GetString(item, "kind"));
            Assert.Equal("open", GetString(item, "status"));
            Assert.Equal(location, GetString(item, "location"));
            Assert.Equal(category, GetString(item, "category"));
            AssertContactAbsent(item);
            AssertManageTokenAbsent(item);
        });
        AssertListBodyHasNoContact(body);

        using var missing = await client.GetAsync("api/items/999999999/matches");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Contact_NeverInList_OnlyOnGetItemWithManageToken()
    {
        using var client = ApiClient.NewClient();
        const string contact = "gizli@aksaray.edu.tr";
        var created = await CreateItemAsync(client, new
        {
            title = $"{Marker}-contact",
            description = "contact hiding",
            location = "yemekhane",
            category = "çanta",
            contact,
            kind = "found",
            status = "open"
        });
        var id = created.Id;
        var token = created.ManageToken;

        using (var getOpen = await client.GetAsync($"api/items/{id}"))
        {
            var body = await getOpen.Content.ReadAsStringAsync();
            var item = JsonSerializer.Deserialize<JsonElement>(body, ApiClient.JsonOptions);
            Assert.Equal("open", GetString(item, "status"));
            AssertContactAbsent(item);
            AssertManageTokenAbsent(item);
        }

        using (var getOpenOwned = await SendAsync(client, HttpMethod.Get, $"api/items/{id}", manageToken: token))
        {
            var body = await getOpenOwned.Content.ReadAsStringAsync();
            var item = JsonSerializer.Deserialize<JsonElement>(body, ApiClient.JsonOptions);
            Assert.Equal(contact, GetString(item, "contact"));
            AssertManageTokenAbsent(item);
        }

        using (var list = await client.GetAsync($"api/items?q={Uri.EscapeDataString(Marker + "-contact")}"))
        {
            var body = await list.Content.ReadAsStringAsync();
            var items = JsonSerializer.Deserialize<List<JsonElement>>(body, ApiClient.JsonOptions);
            var item = Assert.Single(items!, i => i.GetProperty("id").GetInt32() == id);
            AssertContactAbsent(item);
            AssertListBodyHasNoContact(body);
            Assert.DoesNotContain(contact, body, StringComparison.Ordinal);
        }

        await PatchStatusAsync(client, id, "claimed", HttpStatusCode.OK, token);

        using (var getClaimed = await client.GetAsync($"api/items/{id}"))
        {
            var body = await getClaimed.Content.ReadAsStringAsync();
            var item = JsonSerializer.Deserialize<JsonElement>(body, ApiClient.JsonOptions);
            Assert.Equal("claimed", GetString(item, "status"));
            AssertContactAbsent(item);
        }

        using (var getClaimedOwned = await SendAsync(client, HttpMethod.Get, $"api/items/{id}", manageToken: token))
        {
            var body = await getClaimedOwned.Content.ReadAsStringAsync();
            var item = JsonSerializer.Deserialize<JsonElement>(body, ApiClient.JsonOptions);
            Assert.Equal(contact, GetString(item, "contact"));
        }

        using (var listClaimed = await client.GetAsync($"api/items?status=claimed&q={Uri.EscapeDataString(Marker + "-contact")}"))
        {
            var body = await listClaimed.Content.ReadAsStringAsync();
            var items = JsonSerializer.Deserialize<List<JsonElement>>(body, ApiClient.JsonOptions);
            var item = Assert.Single(items!, i => i.GetProperty("id").GetInt32() == id);
            AssertContactAbsent(item);
            Assert.DoesNotContain(contact, body, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task PatchStatus_WithoutManageToken_Is403()
    {
        using var client = ApiClient.NewClient();
        var created = await CreateItemAsync(client, new
        {
            title = $"{Marker}-no-token-status",
            description = "must not change without token",
            location = "merkez",
            category = "diğer",
            contact = "notoken@aksaray.edu.tr",
            kind = "lost",
            status = "open"
        });

        using (var missing = await client.PatchAsync($"api/items/{created.Id}/status", ApiClient.JsonBody(new { status = "claimed" })))
        {
            var body = await missing.Content.ReadAsStringAsync();
            Assert.True(missing.StatusCode == HttpStatusCode.Forbidden,
                $"PATCH without token expected 403, got {(int)missing.StatusCode}: {ApiClient.Excerpt(body)}");
        }

        using (var wrong = await SendAsync(client, HttpMethod.Patch, $"api/items/{created.Id}/status", new { status = "claimed" }, "ffffffffffffffffffffffffffffffff"))
        {
            var body = await wrong.Content.ReadAsStringAsync();
            Assert.True(wrong.StatusCode == HttpStatusCode.Forbidden,
                $"PATCH with wrong token expected 403, got {(int)wrong.StatusCode}: {ApiClient.Excerpt(body)}");
        }

        using (var get = await client.GetAsync($"api/items/{created.Id}"))
        {
            var body = await get.Content.ReadAsStringAsync();
            var item = JsonSerializer.Deserialize<JsonElement>(body, ApiClient.JsonOptions);
            Assert.Equal("open", GetString(item, "status"));
            AssertContactAbsent(item);
        }
    }

    [Fact]
    public async Task PutAndDelete_WithoutManageToken_Is403()
    {
        using var client = ApiClient.NewClient();
        var payload = new
        {
            title = $"{Marker}-mutate-guard",
            description = "put/delete require token",
            location = "yurt",
            category = "anahtar",
            contact = "mutate@aksaray.edu.tr",
            kind = "found",
            status = "open"
        };
        var created = await CreateItemAsync(client, payload);

        var hijack = new
        {
            payload.title,
            payload.description,
            payload.location,
            payload.category,
            contact = "hijacked@aksaray.edu.tr",
            payload.kind,
            status = "claimed"
        };

        using (var put = await client.PutAsync($"api/items/{created.Id}", ApiClient.JsonBody(hijack)))
        {
            var body = await put.Content.ReadAsStringAsync();
            Assert.True(put.StatusCode == HttpStatusCode.Forbidden,
                $"PUT without token expected 403, got {(int)put.StatusCode}: {ApiClient.Excerpt(body)}");
        }

        using (var del = await client.DeleteAsync($"api/items/{created.Id}"))
        {
            var body = await del.Content.ReadAsStringAsync();
            Assert.True(del.StatusCode == HttpStatusCode.Forbidden,
                $"DELETE without token expected 403, got {(int)del.StatusCode}: {ApiClient.Excerpt(body)}");
        }

        using (var get = await client.GetAsync($"api/items/{created.Id}"))
        {
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var item = JsonSerializer.Deserialize<JsonElement>(await get.Content.ReadAsStringAsync(), ApiClient.JsonOptions);
            Assert.Equal("open", GetString(item, "status"));
            AssertContactAbsent(item);
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
        string token;
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
            var created = await CreateItemAsync(postClient, payload);
            id = created.Id;
            token = created.ManageToken;
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
            AssertContactAbsent(item);
            Assert.True(item.TryGetProperty("statusHistory", out var history) && history.ValueKind == JsonValueKind.Array);

            using var patch = await SendAsync(afterRestart, HttpMethod.Patch, $"api/items/{id}/status", new { status = "claimed" }, token);
            var patchBody = await patch.Content.ReadAsStringAsync();
            Assert.True(patch.StatusCode == HttpStatusCode.OK,
                $"Manage token hash must survive API restart, got {(int)patch.StatusCode}: {ApiClient.Excerpt(patchBody)}");
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

    private static async Task<CreatedItem> CreateItemAsync(HttpClient client, object payload)
    {
        using var post = await client.PostAsync("api/items", ApiClient.JsonBody(payload));
        var body = await post.Content.ReadAsStringAsync();
        Assert.True(post.StatusCode == HttpStatusCode.Created,
            $"Expected 201 creating helper item, got {(int)post.StatusCode}: {ApiClient.Excerpt(body)}");
        var posted = JsonSerializer.Deserialize<JsonElement>(body, ApiClient.JsonOptions);
        var id = ReadId(posted, $"Helper POST missing id: {ApiClient.Excerpt(body)}");
        var token = ReadManageToken(posted, $"Helper POST missing manageToken: {ApiClient.Excerpt(body)}");
        AssertContactAbsent(posted);
        return new CreatedItem(id, token);
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        object? body = null,
        string? manageToken = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = ApiClient.JsonBody(body);
        }

        if (!string.IsNullOrEmpty(manageToken))
        {
            request.Headers.TryAddWithoutValidation("X-Manage-Token", manageToken);
        }

        return await client.SendAsync(request);
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
        Assert.All(items!, AssertContactAbsent);
        AssertListBodyHasNoContact(body);
    }

    private static async Task PatchStatusAsync(HttpClient client, int id, string status, HttpStatusCode expected, string? manageToken = null)
    {
        using var patch = await SendAsync(client, HttpMethod.Patch, $"api/items/{id}/status", new { status }, manageToken);
        var body = await patch.Content.ReadAsStringAsync();
        Assert.True(patch.StatusCode == expected,
            $"PATCH status={status} expected {(int)expected}, got {(int)patch.StatusCode}: {ApiClient.Excerpt(body)}");
        if (expected == HttpStatusCode.OK)
        {
            var item = JsonSerializer.Deserialize<JsonElement>(body, ApiClient.JsonOptions);
            Assert.Equal(status, GetString(item, "status"));
            AssertContactAbsent(item);
        }
    }

    private static void AssertFieldsMatch(JsonElement item, object expected)
    {
        var json = JsonSerializer.Serialize(expected, ApiClient.JsonOptions);
        using var doc = JsonDocument.Parse(json);

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Name is "contact" or "manageToken")
            {
                continue;
            }

            Assert.True(item.TryGetProperty(prop.Name, out var actual), $"Missing field {prop.Name}");
            Assert.Equal(prop.Value.GetString(), actual.ValueKind == JsonValueKind.Null ? null : actual.GetString());
        }

        AssertContactAbsent(item);
    }

    private static void AssertContactAbsent(JsonElement item)
    {
        if (!item.TryGetProperty("contact", out var contact))
        {
            return;
        }

        Assert.True(contact.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined,
            $"contact must be omitted, got {contact}");
    }

    private static void AssertManageTokenAbsent(JsonElement item)
    {
        if (item.TryGetProperty("manageToken", out var token))
        {
            Assert.True(token.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined,
                $"manageToken must only appear on create, got {token}");
        }

        Assert.False(item.TryGetProperty("manageTokenHash", out _), "manageTokenHash must never be serialized");
    }

    private static void AssertListBodyHasNoContact(string body)
    {
        Assert.DoesNotContain("\"contact\"", body, StringComparison.OrdinalIgnoreCase);
        foreach (var seedContact in SeedContactValues)
        {
            Assert.DoesNotContain(seedContact, body, StringComparison.OrdinalIgnoreCase);
        }
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

    private static string ReadManageToken(JsonElement item, string message)
    {
        var token = GetString(item, "manageToken");
        if (string.IsNullOrWhiteSpace(token) || token.Length != 32)
        {
            Assert.Fail(message);
            return string.Empty;
        }

        Assert.Matches("^[0-9a-f]{32}$", token);
        return token;
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

    private sealed record CreatedItem(int Id, string ManageToken);
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
