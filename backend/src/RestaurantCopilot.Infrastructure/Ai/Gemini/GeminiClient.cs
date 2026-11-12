using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Infrastructure.Ai.Gemini;

/// <summary><see cref="ILlmClient"/> backed by the Gemini <c>generateContent</c> REST endpoint.</summary>
internal sealed class GeminiClient(HttpClient httpClient, IOptions<GeminiOptions> options, ILogger<GeminiClient> logger)
    : ILlmClient
{
    private readonly GeminiOptions _options = options.Value;

    public async Task<Result<LlmResponse>> GenerateAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            return LlmErrors.NotConfigured;
        }

        var stopwatch = Stopwatch.StartNew();
        var result = await GeminiHttp.PostAsync<GenerateContentRequest, GenerateContentResponse>(
            httpClient,
            $"models/{_options.GenerationModel}:generateContent",
            GeminiRequestMapper.ToGenerateContentRequest(request),
            logger,
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error;
        }

        var response = result.Value;
        var candidate = response.Candidates is [var first, ..] ? first : null;
        if (candidate is null || candidate.FinishReason is "SAFETY" or "PROHIBITED_CONTENT" or "BLOCKLIST")
        {
            logger.LogWarning(
                "Gemini blocked the request (block reason: {BlockReason}, finish reason: {FinishReason})",
                response.PromptFeedback?.BlockReason,
                candidate?.FinishReason);
            return LlmErrors.Blocked;
        }

        var llmResponse = GeminiRequestMapper.ToLlmResponse(candidate.Content, response.UsageMetadata);
        logger.LogInformation(
            "Gemini {Model} answered in {ElapsedMs} ms ({InputTokens} input / {OutputTokens} output tokens, {ToolCallCount} tool calls)",
            _options.GenerationModel,
            stopwatch.ElapsedMilliseconds,
            llmResponse.Usage.InputTokens,
            llmResponse.Usage.OutputTokens,
            llmResponse.ToolCalls.Count);

        return llmResponse;
    }
}
