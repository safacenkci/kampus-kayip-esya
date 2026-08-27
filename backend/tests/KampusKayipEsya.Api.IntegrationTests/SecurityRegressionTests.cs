using System.Net;
using System.Text.Json;
using KampusKayipEsya.Api.IntegrationTests.Infrastructure;

namespace KampusKayipEsya.Api.IntegrationTests;

public sealed class SecurityRegressionTests : IntegrationTestBase
{
    private static readonly string Marker = $"it-sec-{Guid.NewGuid():N}";

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

    public SecurityRegressionTests(PostgresApiFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task PatchStatus_WithoutManageToken_Is403()
    {
        var (id, _, _) = await CreateItemAsync(Client, new
        {
            title = $"{Marker}-notoken",
            location = "yemekhane",
            category = "çanta",
            contact = "notoken@aksaray.edu.tr",
            kind = "found"
        });

        using (var missing = await SendAsync(Client, HttpMethod.Patch, $"api/items/{id}/status", new { status = "claimed" }))
        {
            var body = await missing.Content.ReadAsStringAsync();
            Assert.True(missing.StatusCode == HttpStatusCode.Forbidden,
                $"PATCH without token expected 403, got {(int)missing.StatusCode}: {Excerpt(body)}");
        }

        using (var wrong = await SendAsync(
            Client,
            HttpMethod.Patch,
            $"api/items/{id}/status",
            new { status = "claimed" },
            manageToken: "ffffffffffffffffffffffffffffffff"))
        {
            var body = await wrong.Content.ReadAsStringAsync();
            Assert.True(wrong.StatusCode == HttpStatusCode.Forbidden,
                $"PATCH with wrong token expected 403, got {(int)wrong.StatusCode}: {Excerpt(body)}");
        }

        using var get = await Client.GetAsync($"api/items/{id}");
        var getBody = await get.Content.ReadAsStringAsync();
        var item = JsonSerializer.Deserialize<JsonElement>(getBody, JsonOptions);
        Assert.Equal("open", ReadString(item, "status"));
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task GetItems_NeverIncludesContact_ForOpenClaimedClosed()
    {
        var (openId, _, _) = await CreateItemAsync(Client, new
        {
            title = $"{Marker}-open",
            location = "merkez",
            category = "diğer",
            contact = "open-secret@aksaray.edu.tr",
            kind = "lost",
            status = "open"
        });
        var (claimedId, claimedToken, _) = await CreateItemAsync(Client, new
        {
            title = $"{Marker}-claimed",
            location = "kütüphane",
            category = "telefon",
            contact = "claimed-secret@aksaray.edu.tr",
            kind = "found",
            status = "open"
        });
        using (var claimed = await SendAsync(Client, HttpMethod.Patch, $"api/items/{claimedId}/status", new { status = "claimed" }, claimedToken))
        {
            Assert.Equal(HttpStatusCode.OK, claimed.StatusCode);
        }

        var (closedId, closedToken, _) = await CreateItemAsync(Client, new
        {
            title = $"{Marker}-closed",
            location = "yurt",
            category = "anahtar",
            contact = "closed-secret@aksaray.edu.tr",
            kind = "lost",
            status = "open"
        });
        using (var toClaimed = await SendAsync(Client, HttpMethod.Patch, $"api/items/{closedId}/status", new { status = "claimed" }, closedToken))
        {
            Assert.Equal(HttpStatusCode.OK, toClaimed.StatusCode);
        }

        using (var toClosed = await SendAsync(Client, HttpMethod.Patch, $"api/items/{closedId}/status", new { status = "closed" }, closedToken))
        {
            Assert.Equal(HttpStatusCode.OK, toClosed.StatusCode);
        }

        using var list = await Client.GetAsync("api/items");
        var body = await list.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        AssertListHasNoContact(body);

        foreach (var email in SeedContactValues)
        {
            Assert.DoesNotContain(email, body, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("open-secret@aksaray.edu.tr", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("claimed-secret@aksaray.edu.tr", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("closed-secret@aksaray.edu.tr", body, StringComparison.OrdinalIgnoreCase);

        var items = JsonSerializer.Deserialize<List<JsonElement>>(body, JsonOptions);
        Assert.NotNull(items);
        Assert.Contains(items!, i => i.GetProperty("id").GetInt32() == openId && ReadString(i, "status") == "open");
        Assert.Contains(items!, i => i.GetProperty("id").GetInt32() == claimedId && ReadString(i, "status") == "claimed");
        Assert.Contains(items!, i => i.GetProperty("id").GetInt32() == closedId && ReadString(i, "status") == "closed");
        Assert.Contains(items!, i => string.Equals(ReadString(i, "status"), "open", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(items!, i => string.Equals(ReadString(i, "status"), "claimed", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(items!, i => string.Equals(ReadString(i, "status"), "closed", StringComparison.OrdinalIgnoreCase));
        Assert.All(items!, AssertNoContact);
    }
}
