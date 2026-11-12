using Microsoft.Extensions.Options;
using RestaurantCopilot.Infrastructure.Ai.Gemini;

namespace RestaurantCopilot.Infrastructure.Tests.Ai;

internal static class GeminiTestData
{
    public static readonly GeminiOptions Options = new()
    {
        ApiKey = "test-key",
        GenerationModel = "test-flash",
        EmbeddingModel = "test-embedding",
        EmbeddingDimensions = 3,
    };

    public static HttpClient CreateHttpClient(FakeHttpMessageHandler handler) =>
        new(handler) { BaseAddress = Options.BaseUrl };

    public static IOptions<GeminiOptions> Wrap(GeminiOptions options) => Microsoft.Extensions.Options.Options.Create(options);

    public static string TextResponse(string text) => $$"""
        {
          "candidates": [{ "content": { "role": "model", "parts": [{ "text": {{System.Text.Json.JsonSerializer.Serialize(text)}} }] }, "finishReason": "STOP" }],
          "usageMetadata": { "promptTokenCount": 12, "candidatesTokenCount": 5 }
        }
        """;
}
