using System.Text.Json;
using RestaurantCopilot.Application.Abstractions.Ai;

namespace RestaurantCopilot.Infrastructure.Ai.Gemini;

/// <summary>Translates the provider-agnostic <see cref="LlmRequest"/> into Gemini's wire format and back.</summary>
internal static class GeminiRequestMapper
{
    private const string UserRole = "user";
    private const string ModelRole = "model";

    private static readonly JsonElement EmptyObject = JsonDocument.Parse("{}").RootElement.Clone();

    public static GenerateContentRequest ToGenerateContentRequest(LlmRequest request)
    {
        var tools = request.Tools.Count == 0
            ? null
            : new[]
            {
                new GeminiTool(request.Tools
                    .Select(tool => new GeminiFunctionDeclaration(tool.Name, tool.Description, tool.ParametersSchema))
                    .ToList()),
            };

        var generationConfig = request.ResponseJsonSchema is null && request.Temperature is null
            ? null
            : new GeminiGenerationConfig(
                request.Temperature,
                request.ResponseJsonSchema is null ? null : "application/json",
                request.ResponseJsonSchema);

        return new GenerateContentRequest(
            request.Messages.Select(ToContent).ToList(),
            request.SystemInstruction is null ? null : new GeminiContent(null, [new GeminiPart { Text = request.SystemInstruction }]),
            tools,
            generationConfig);
    }

    public static LlmResponse ToLlmResponse(GeminiContent? content, GeminiUsageMetadata? usage)
    {
        var parts = content?.Parts ?? [];
        var answerParts = parts.Where(part => part.Thought is not true && part.Text is not null).Select(part => part.Text);
        var text = string.Concat(answerParts);

        var toolCalls = parts
            .Where(part => part.FunctionCall is not null)
            .Select(part => new LlmToolCall(
                part.FunctionCall!.Name,
                part.FunctionCall.Args ?? EmptyObject,
                part.FunctionCall.Id,
                part.ThoughtSignature))
            .ToList();

        return new LlmResponse(
            text.Length == 0 ? null : text,
            toolCalls,
            usage is null ? LlmUsage.None : new LlmUsage(usage.PromptTokenCount, usage.CandidatesTokenCount));
    }

    private static GeminiContent ToContent(LlmMessage message) => message switch
    {
        UserMessage user => new GeminiContent(UserRole, [new GeminiPart { Text = user.Text }]),
        AssistantMessage assistant => new GeminiContent(ModelRole, ToParts(assistant)),
        ToolResultMessage toolResult => new GeminiContent(UserRole,
        [
            new GeminiPart
            {
                FunctionResponse = new GeminiFunctionResponse(toolResult.ToolName, WrapAsObject(toolResult.Result), toolResult.CallId),
            },
        ]),
        _ => throw new ArgumentOutOfRangeException(nameof(message), message.GetType().Name, "Unsupported message type."),
    };

    private static List<GeminiPart> ToParts(AssistantMessage assistant)
    {
        var parts = new List<GeminiPart>();
        if (!string.IsNullOrEmpty(assistant.Text))
        {
            parts.Add(new GeminiPart { Text = assistant.Text });
        }

        parts.AddRange(assistant.ToolCalls.Select(call => new GeminiPart
        {
            FunctionCall = new GeminiFunctionCall(call.Name, call.Arguments, call.CallId),
            ThoughtSignature = call.ProviderState,
        }));

        return parts;
    }

    /// <summary>Gemini requires a function response to be a JSON object; other values are wrapped.</summary>
    private static JsonElement WrapAsObject(JsonElement value) =>
        value.ValueKind == JsonValueKind.Object
            ? value
            : JsonSerializer.SerializeToElement(new { result = value });
}
