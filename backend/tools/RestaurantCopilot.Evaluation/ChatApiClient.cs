using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RestaurantCopilot.Evaluation;

/// <summary>Calls the running API exactly like a client would, so the evaluation covers the whole stack.</summary>
internal sealed class ChatApiClient(HttpClient httpClient)
{
    public async Task<ChatOutcome> AskAsync(string question, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/chat")
        {
            Content = JsonContent.Create(new { question }),
        };
        // Measure the agent, not the cache.
        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };

        var stopwatch = Stopwatch.StartNew();
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var elapsed = stopwatch.Elapsed;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new ChatOutcome(null, [], 0, 0, elapsed, $"HTTP {(int)response.StatusCode}: {body}");
        }

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        var usage = root.GetProperty("usage");
        return new ChatOutcome(
            root.GetProperty("answer").GetString(),
            root.GetProperty("toolCalls").EnumerateArray().Select(call => call.GetProperty("tool").GetString()!).ToList(),
            usage.GetProperty("inputTokens").GetInt32(),
            usage.GetProperty("outputTokens").GetInt32(),
            elapsed,
            Error: null);
    }

    public async Task PostAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync(path, content: null, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Console.WriteLine($"POST {path} -> {(int)response.StatusCode} {body}");
        response.EnsureSuccessStatusCode();
    }
}

internal sealed record ChatOutcome(
    string? Answer,
    IReadOnlyList<string> ToolsUsed,
    int InputTokens,
    int OutputTokens,
    TimeSpan Elapsed,
    string? Error);
