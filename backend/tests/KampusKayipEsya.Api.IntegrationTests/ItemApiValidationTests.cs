using System.Net;
using KampusKayipEsya.Api.IntegrationTests.Infrastructure;

namespace KampusKayipEsya.Api.IntegrationTests;

public sealed class ItemApiValidationTests : IntegrationTestBase
{
    private static readonly string Marker = $"it-val-{Guid.NewGuid():N}";

    public ItemApiValidationTests(PostgresApiFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task Post_MissingTitle_Is400()
    {
        using var response = await Client.PostAsync("api/items", JsonBody(new
        {
            location = "merkez",
            category = "anahtar",
            kind = "lost"
        }));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 400 for missing title, got {(int)response.StatusCode}: {Excerpt(body)}");
    }

    [Fact]
    public async Task Post_InvalidLocationOrCategoryOrKind_Is400()
    {
        using (var postLoc = await Client.PostAsync("api/items", JsonBody(new
        {
            title = $"{Marker}-bad-loc",
            location = "Rektörlük",
            category = "anahtar",
            kind = "lost"
        })))
        {
            var body = await postLoc.Content.ReadAsStringAsync();
            Assert.True(postLoc.StatusCode == HttpStatusCode.BadRequest,
                $"Expected 400 for invalid location, got {(int)postLoc.StatusCode}: {Excerpt(body)}");
        }

        using (var postCat = await Client.PostAsync("api/items", JsonBody(new
        {
            title = $"{Marker}-bad-cat",
            location = "merkez",
            category = "giyim",
            kind = "found"
        })))
        {
            var body = await postCat.Content.ReadAsStringAsync();
            Assert.True(postCat.StatusCode == HttpStatusCode.BadRequest,
                $"Expected 400 for invalid category, got {(int)postCat.StatusCode}: {Excerpt(body)}");
        }

        using (var postKind = await Client.PostAsync("api/items", JsonBody(new
        {
            title = $"{Marker}-bad-kind",
            location = "merkez",
            category = "anahtar",
            kind = "other"
        })))
        {
            var body = await postKind.Content.ReadAsStringAsync();
            Assert.True(postKind.StatusCode == HttpStatusCode.BadRequest,
                $"Expected 400 for invalid kind, got {(int)postKind.StatusCode}: {Excerpt(body)}");
        }
    }

    [Fact]
    public async Task GetItems_InvalidFilters_Is400()
    {
        using (var filterLoc = await Client.GetAsync("api/items?location=Rektörlük"))
        {
            Assert.Equal(HttpStatusCode.BadRequest, filterLoc.StatusCode);
        }

        using (var filterCat = await Client.GetAsync("api/items?category=giyim"))
        {
            Assert.Equal(HttpStatusCode.BadRequest, filterCat.StatusCode);
        }

        using (var filterKind = await Client.GetAsync("api/items?kind=maybe"))
        {
            Assert.Equal(HttpStatusCode.BadRequest, filterKind.StatusCode);
        }

        using (var filterStatus = await Client.GetAsync("api/items?status=pending"))
        {
            Assert.Equal(HttpStatusCode.BadRequest, filterStatus.StatusCode);
        }
    }

    [Fact]
    public async Task PatchStatus_InvalidStatus_Is400()
    {
        var (id, token, _) = await CreateItemAsync(Client, new
        {
            title = $"{Marker}-status",
            location = "spor salonu",
            category = "kıyafet",
            kind = "lost"
        });

        using var invalid = await SendAsync(
            Client,
            HttpMethod.Patch,
            $"api/items/{id}/status",
            new { status = "not-a-status" },
            token);
        var body = await invalid.Content.ReadAsStringAsync();
        Assert.True(invalid.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 400 for invalid status, got {(int)invalid.StatusCode}: {Excerpt(body)}");
    }

    [Fact]
    public async Task UnknownItem_GetPatchPutDeleteMatches_Is404()
    {
        const int unknownId = 999999999;

        using (var get = await Client.GetAsync($"api/items/{unknownId}"))
        {
            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        using (var matches = await Client.GetAsync($"api/items/{unknownId}/matches"))
        {
            Assert.Equal(HttpStatusCode.NotFound, matches.StatusCode);
        }

        using (var patch = await SendAsync(Client, HttpMethod.Patch, $"api/items/{unknownId}/status", new { status = "claimed" }))
        {
            Assert.Equal(HttpStatusCode.NotFound, patch.StatusCode);
        }

        using (var put = await SendAsync(Client, HttpMethod.Put, $"api/items/{unknownId}", new
        {
            title = $"{Marker}-missing",
            location = "merkez",
            category = "anahtar",
            kind = "lost"
        }))
        {
            Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        }

        using (var del = await SendAsync(Client, HttpMethod.Delete, $"api/items/{unknownId}"))
        {
            Assert.Equal(HttpStatusCode.NotFound, del.StatusCode);
        }
    }
}
