using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Infrastructure.Ai.Gemini;

/// <summary><see cref="IEmbeddingService"/> backed by the Gemini <c>batchEmbedContents</c> endpoint.</summary>
internal sealed class GeminiEmbeddingService(
    HttpClient httpClient,
    IOptions<GeminiOptions> options,
    ILogger<GeminiEmbeddingService> logger) : IEmbeddingService
{
    /// <summary>Maximum number of texts Gemini accepts in one batch request.</summary>
    internal const int MaxBatchSize = 100;

    private readonly GeminiOptions _options = options.Value;

    public int Dimensions => _options.EmbeddingDimensions;

    public async Task<Result<IReadOnlyList<float[]>>> EmbedAsync(
        IReadOnlyList<string> texts,
        EmbeddingPurpose purpose,
        CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            return LlmErrors.NotConfigured;
        }

        var vectors = new List<float[]>(texts.Count);
        foreach (var batch in texts.Chunk(MaxBatchSize))
        {
            var result = await EmbedBatchAsync(batch, purpose, cancellationToken);
            if (result.IsFailure)
            {
                return result.Error;
            }

            vectors.AddRange(result.Value);
        }

        return vectors;
    }

    private async Task<Result<IReadOnlyList<float[]>>> EmbedBatchAsync(
        string[] texts,
        EmbeddingPurpose purpose,
        CancellationToken cancellationToken)
    {
        var model = $"models/{_options.EmbeddingModel}";
        var taskType = purpose == EmbeddingPurpose.Query ? "RETRIEVAL_QUERY" : "RETRIEVAL_DOCUMENT";
        var request = new BatchEmbedContentsRequest(texts
            .Select(text => new EmbedContentRequest(model, new GeminiContent(null, [new GeminiPart { Text = text }]), taskType, Dimensions))
            .ToList());

        var result = await GeminiHttp.PostAsync<BatchEmbedContentsRequest, BatchEmbedContentsResponse>(
            httpClient, $"{model}:batchEmbedContents", request, logger, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error;
        }

        var embeddings = result.Value.Embeddings ?? [];
        if (embeddings.Count != texts.Length || embeddings.Any(embedding => embedding.Values.Count != Dimensions))
        {
            return LlmErrors.InvalidResponse(
                $"Expected {texts.Length} embeddings of {Dimensions} dimensions from Gemini.");
        }

        return embeddings.Select(embedding => embedding.Values.ToArray()).ToList();
    }
}
