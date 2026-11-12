using System.Text.Json;
using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Tests.TestDoubles;

/// <summary>Plays back a fixed script of model turns and records every request it receives.</summary>
internal sealed class ScriptedLlmClient(params Func<LlmRequest, Result<LlmResponse>>[] turns) : ILlmClient
{
    private int _turn;

    public List<LlmRequest> Requests { get; } = [];

    public Task<Result<LlmResponse>> GenerateAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var turn = turns[Math.Min(_turn++, turns.Length - 1)];
        return Task.FromResult(turn(request));
    }

    public static Func<LlmRequest, Result<LlmResponse>> Answer(string text) =>
        _ => new LlmResponse(text, [], new LlmUsage(100, 20));

    public static Func<LlmRequest, Result<LlmResponse>> CallTool(string name, string argumentsJson) =>
        _ => new LlmResponse(null, [new LlmToolCall(name, JsonDocument.Parse(argumentsJson).RootElement, CallId: $"call-{name}")], new LlmUsage(80, 10));
}
