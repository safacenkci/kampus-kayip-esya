using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KampusKayipEsya.Api.Tests;

internal static class ApiClient
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    internal static string BaseUrl =>
        Environment.GetEnvironmentVariable("API_BASE_URL")?.TrimEnd('/')
        ?? "http://localhost:5080";

    internal static HttpClient NewClient()
    {
        var client = new HttpClient { BaseAddress = new Uri(BaseUrl + "/"), Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    internal static StringContent JsonBody(object value) =>
        new(JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8, "application/json");

    internal static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(text))
        {
            return default;
        }

        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.Clone();
    }

    internal static string Excerpt(string? body, int max = 240)
    {
        if (string.IsNullOrEmpty(body))
        {
            return "(empty body)";
        }

        var trimmed = body.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max] + "...";
    }
}
