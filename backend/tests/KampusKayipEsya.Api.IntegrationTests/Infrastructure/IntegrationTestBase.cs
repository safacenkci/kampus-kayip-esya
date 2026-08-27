using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KampusKayipEsya.Api.IntegrationTests.Infrastructure;

[Collection(IntegrationCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly PostgresApiFixture _fixture;

    protected HttpClient Client { get; private set; } = null!;

    protected IntegrationTestBase(PostgresApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        Client = _fixture.Factory.CreateClient();
        Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    protected static StringContent JsonBody(object value) =>
        new(JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8, "application/json");

    protected static async Task<(int Id, string ManageToken, JsonElement Body)> CreateItemAsync(
        HttpClient client,
        object payload)
    {
        using var response = await client.PostAsync("api/items", JsonBody(payload));
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created,
            $"Expected 201 from POST /api/items, got {(int)response.StatusCode}: {Excerpt(text)}");

        var body = JsonSerializer.Deserialize<JsonElement>(text, JsonOptions);
        var id = ReadInt(body, "id");
        var token = ReadString(body, "manageToken");
        Assert.False(string.IsNullOrWhiteSpace(token), $"POST 201 body missing manageToken: {Excerpt(text)}");
        return (id, token!, body);
    }

    protected static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        object? payload = null,
        string? manageToken = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (payload is not null)
        {
            request.Content = JsonBody(payload);
        }

        if (!string.IsNullOrEmpty(manageToken))
        {
            request.Headers.TryAddWithoutValidation("X-Manage-Token", manageToken);
        }

        return await client.SendAsync(request);
    }

    protected static int ReadInt(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value) || !value.TryGetInt32(out var id) || id <= 0)
        {
            Assert.Fail($"Expected positive int '{name}'");
            return 0;
        }

        return id;
    }

    protected static string? ReadString(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.GetString()
            : null;

    protected static void AssertNoContact(JsonElement item)
    {
        if (!item.TryGetProperty("contact", out var contact))
        {
            return;
        }

        Assert.True(contact.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined,
            $"contact must be omitted, got {contact}");
    }

    protected static void AssertNoManageToken(JsonElement item)
    {
        if (item.TryGetProperty("manageToken", out var token))
        {
            Assert.True(token.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined,
                $"manageToken must only appear on create, got {token}");
        }

        Assert.False(item.TryGetProperty("manageTokenHash", out _), "manageTokenHash must never be serialized");
    }

    protected static void AssertListHasNoContact(string body)
    {
        Assert.DoesNotContain("\"contact\"", body, StringComparison.OrdinalIgnoreCase);
    }

    protected static void AssertHistoryContains(JsonElement item, string from, string to)
    {
        Assert.True(
            item.TryGetProperty("statusHistory", out var history) && history.ValueKind == JsonValueKind.Array,
            "Expected statusHistory array");
        Assert.Contains(history.EnumerateArray(), entry =>
            string.Equals(ReadString(entry, "from"), from, StringComparison.OrdinalIgnoreCase)
            && string.Equals(ReadString(entry, "to"), to, StringComparison.OrdinalIgnoreCase)
            && ReadString(entry, "changedAt") is not null);
    }

    protected static string Excerpt(string? body, int max = 240)
    {
        if (string.IsNullOrEmpty(body))
        {
            return "(empty body)";
        }

        var trimmed = body.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max] + "...";
    }
}
