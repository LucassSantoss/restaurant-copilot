using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Infrastructure.Ai.Gemini;

namespace RestaurantCopilot.Infrastructure.Tests.Ai;

public sealed class GeminiEmbeddingServiceTests
{
    private readonly FakeHttpMessageHandler _handler = new();

    [Fact]
    public async Task EmbedAsync_SplitsLargeInputsIntoBatchesAndKeepsOrder()
    {
        var texts = Enumerable.Range(0, GeminiEmbeddingService.MaxBatchSize + 1).Select(i => $"texto {i}").ToList();
        _handler
            .RespondWith(HttpStatusCode.OK, EmbeddingsResponse(GeminiEmbeddingService.MaxBatchSize, startAt: 0))
            .RespondWith(HttpStatusCode.OK, EmbeddingsResponse(1, startAt: GeminiEmbeddingService.MaxBatchSize));

        var result = await CreateService().EmbedAsync(texts, EmbeddingPurpose.Query, TestContext.Current.CancellationToken);

        _handler.Requests.Count.ShouldBe(2);
        _handler.Requests[0].Request.RequestUri!.AbsolutePath.ShouldEndWith("/models/test-embedding:batchEmbedContents");
        result.Value.Count.ShouldBe(texts.Count);
        result.Value[^1][0].ShouldBe(GeminiEmbeddingService.MaxBatchSize);

        using var body = JsonDocument.Parse(_handler.Requests[0].Body);
        var first = body.RootElement.GetProperty("requests")[0];
        first.GetProperty("taskType").GetString().ShouldBe("RETRIEVAL_QUERY");
        first.GetProperty("outputDimensionality").GetInt32().ShouldBe(3);
        first.GetProperty("model").GetString().ShouldBe("models/test-embedding");
    }

    [Fact]
    public async Task EmbedAsync_WithUnexpectedDimensions_ReturnsInvalidResponse()
    {
        _handler.RespondWith(HttpStatusCode.OK, """{ "embeddings": [{ "values": [0.1, 0.2] }] }""");

        var result = await CreateService().EmbedAsync(["texto"], EmbeddingPurpose.Document, TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("Llm.InvalidResponse");
    }

    private GeminiEmbeddingService CreateService() =>
        new(GeminiTestData.CreateHttpClient(_handler), GeminiTestData.Wrap(GeminiTestData.Options), NullLogger<GeminiEmbeddingService>.Instance);

    // Each vector starts with its global index, so the test can check ordering across batches.
    private static string EmbeddingsResponse(int count, int startAt) =>
        JsonSerializer.Serialize(new
        {
            embeddings = Enumerable.Range(startAt, count).Select(i => new { values = new float[] { i, 0, 0 } }),
        });
}
