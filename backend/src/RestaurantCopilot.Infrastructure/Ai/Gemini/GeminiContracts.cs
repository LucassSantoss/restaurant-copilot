using System.Text.Json;

namespace RestaurantCopilot.Infrastructure.Ai.Gemini;

// Wire format of the Gemini REST API (generateContent / batchEmbedContents), limited to the
// fields this application uses. See https://ai.google.dev/api.

internal sealed record GenerateContentRequest(
    IReadOnlyList<GeminiContent> Contents,
    GeminiContent? SystemInstruction,
    IReadOnlyList<GeminiTool>? Tools,
    GeminiGenerationConfig? GenerationConfig);

internal sealed record GeminiContent(string? Role, IReadOnlyList<GeminiPart> Parts);

internal sealed record GeminiPart
{
    public string? Text { get; init; }

    /// <summary>True for reasoning summaries, which are not part of the answer.</summary>
    public bool? Thought { get; init; }

    public string? ThoughtSignature { get; init; }

    public GeminiFunctionCall? FunctionCall { get; init; }

    public GeminiFunctionResponse? FunctionResponse { get; init; }
}

internal sealed record GeminiFunctionCall(string Name, JsonElement? Args, string? Id = null);

internal sealed record GeminiFunctionResponse(string Name, JsonElement Response, string? Id = null);

internal sealed record GeminiTool(IReadOnlyList<GeminiFunctionDeclaration> FunctionDeclarations);

internal sealed record GeminiFunctionDeclaration(string Name, string Description, JsonElement ParametersJsonSchema);

internal sealed record GeminiGenerationConfig(
    double? Temperature,
    string? ResponseMimeType,
    JsonElement? ResponseJsonSchema);

internal sealed record GenerateContentResponse(
    IReadOnlyList<GeminiCandidate>? Candidates,
    GeminiPromptFeedback? PromptFeedback,
    GeminiUsageMetadata? UsageMetadata);

internal sealed record GeminiCandidate(GeminiContent? Content, string? FinishReason);

internal sealed record GeminiPromptFeedback(string? BlockReason);

internal sealed record GeminiUsageMetadata(int PromptTokenCount, int CandidatesTokenCount);

internal sealed record BatchEmbedContentsRequest(IReadOnlyList<EmbedContentRequest> Requests);

internal sealed record EmbedContentRequest(string Model, GeminiContent Content, string TaskType, int OutputDimensionality);

internal sealed record BatchEmbedContentsResponse(IReadOnlyList<GeminiEmbedding>? Embeddings);

internal sealed record GeminiEmbedding(IReadOnlyList<float> Values);

internal sealed record GeminiErrorResponse(GeminiError? Error);

internal sealed record GeminiError(int Code, string? Message, string? Status);
