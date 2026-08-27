using System.Diagnostics;
using System.Net.Sockets;

namespace KampusKayipEsya.Api.Tests;

internal static class ApiProcess
{
    internal static async Task RestartAsync()
    {
        var projectDir = FindApiProjectDir();
        await KillListenerOn5080Async();

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --project KampusKayipEsya.Api.csproj --urls http://localhost:5080",
            WorkingDirectory = projectDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        psi.Environment["ASPNETCORE_URLS"] = "http://localhost:5080";

        var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start API process.");

        _ = DrainAsync(process.StandardOutput);
        _ = DrainAsync(process.StandardError);

        await WaitUntilReadyAsync();
    }

    internal static async Task WaitUntilReadyAsync(TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(60));
        Exception? last = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var client = ApiClient.NewClient();
                using var response = await client.GetAsync("api/categories");
                if ((int)response.StatusCode < 500)
                {
                    return;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or SocketException or TaskCanceledException)
            {
                last = ex;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"API did not become ready at {ApiClient.BaseUrl}. Last error: {last?.Message}");
    }

    private static string FindApiProjectDir()
    {
        var env = Environment.GetEnvironmentVariable("API_PROJECT_DIR");
        if (!string.IsNullOrWhiteSpace(env) && File.Exists(Path.Combine(env, "KampusKayipEsya.Api.csproj")))
        {
            return env;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var direct = Path.Combine(dir.FullName, "KampusKayipEsya.Api.csproj");
            if (File.Exists(direct))
            {
                return dir.FullName;
            }

            var sibling = Path.Combine(dir.FullName, "..", "KampusKayipEsya.Api.csproj");
            if (File.Exists(sibling))
            {
                return Path.GetFullPath(Path.Combine(dir.FullName, ".."));
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate KampusKayipEsya.Api.csproj. Set API_PROJECT_DIR.");
    }

    private static async Task KillListenerOn5080Async()
    {
        var psi = new ProcessStartInfo("bash")
        {
            Arguments = "-lc \"if command -v fuser >/dev/null; then fuser -k 5080/tcp || true; fi; if command -v lsof >/dev/null; then lsof -ti:5080 | xargs -r kill || true; fi; sleep 1\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var process = Process.Start(psi);
        if (process is not null)
        {
            await process.WaitForExitAsync();
        }
    }

    private static async Task DrainAsync(StreamReader reader)
    {
        try
        {
            await reader.ReadToEndAsync();
        }
        catch
        {
            // ignored — process may exit while draining
        }
    }
}
