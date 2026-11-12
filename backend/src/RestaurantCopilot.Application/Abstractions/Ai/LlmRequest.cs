using System.Text.Json;

namespace RestaurantCopilot.Application.Abstractions.Ai;

public sealed record LlmRequest(string? SystemInstruction, IReadOnlyList<LlmMessage> Messages)
{
    /// <summary>Functions the model may ask the application to call.</summary>
    public IReadOnlyList<LlmToolDeclaration> Tools { get; init; } = [];

    /// <summary>When set, the model must answer with JSON matching this JSON Schema.</summary>
    public JsonElement? ResponseJsonSchema { get; init; }

    /// <summary>Sampling temperature; null uses the provider default.</summary>
    public double? Temperature { get; init; }
}

/// <summary>A function the model can call. <see cref="ParametersSchema"/> is a JSON Schema object.</summary>
public sealed record LlmToolDeclaration(string Name, string Description, JsonElement ParametersSchema);

/// <summary>A turn of the conversation sent to the model.</summary>
public abstract record LlmMessage;

public sealed record UserMessage(string Text) : LlmMessage;

/// <summary>A previous model turn, replayed so the model sees the tool calls it made.</summary>
public sealed record AssistantMessage(string? Text, IReadOnlyList<LlmToolCall> ToolCalls) : LlmMessage;

/// <summary>The application's answer to a tool call; <see cref="Result"/> is any JSON value.</summary>
public sealed record ToolResultMessage(string ToolName, JsonElement Result, string? CallId = null) : LlmMessage;
