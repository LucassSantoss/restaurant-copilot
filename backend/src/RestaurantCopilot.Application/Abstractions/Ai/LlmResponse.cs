using System.Text.Json;

namespace RestaurantCopilot.Application.Abstractions.Ai;

public sealed record LlmResponse(string? Text, IReadOnlyList<LlmToolCall> ToolCalls, LlmUsage Usage)
{
    public bool HasToolCalls => ToolCalls.Count > 0;
}

/// <param name="Name">Name of the requested tool.</param>
/// <param name="Arguments">Arguments chosen by the model, as a JSON object.</param>
/// <param name="CallId">Provider id that links the call to its result, when the provider uses one.</param>
/// <param name="ProviderState">
/// Opaque provider data (e.g. a reasoning signature) that must be sent back unchanged when
/// the call is replayed in the conversation. Callers never inspect it.
/// </param>
public sealed record LlmToolCall(string Name, JsonElement Arguments, string? CallId = null, string? ProviderState = null);

public sealed record LlmUsage(int InputTokens, int OutputTokens)
{
    public static readonly LlmUsage None = new(0, 0);

    public int TotalTokens => InputTokens + OutputTokens;
}
