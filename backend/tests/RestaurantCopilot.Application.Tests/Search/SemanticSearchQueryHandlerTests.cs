using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Application.Abstractions.Search;
using RestaurantCopilot.Application.Search.SemanticSearch;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Tests.Search;

public sealed class SemanticSearchQueryHandlerTests
{
    private readonly IEmbeddingService _embeddings = Substitute.For<IEmbeddingService>();
    private readonly IVectorStore _vectorStore = Substitute.For<IVectorStore>();

    [Fact]
    public async Task HandleAsync_EmbedsQueryAsQueryAndSearchesStore()
    {
        float[] queryVector = [0.1f, 0.2f, 0.3f];
        var sourceId = Guid.NewGuid();
        _embeddings.EmbedAsync(Arg.Is<IReadOnlyList<string>>(texts => texts.Single() == "embalagem"), EmbeddingPurpose.Query, Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<float[]>>([queryVector]));
        _vectorStore.SearchAsync(queryVector, 3, DocumentSource.Review, Arg.Any<CancellationToken>())
            .Returns([new VectorSearchResult(DocumentSource.Review, sourceId, "Vazou tudo.", 0.876543)]);
        var handler = new SemanticSearchQueryHandler(_embeddings, _vectorStore);

        var result = await handler.HandleAsync(new SemanticSearchQuery("embalagem", 3, DocumentSource.Review), TestContext.Current.CancellationToken);

        result.Value.ShouldHaveSingleItem().ShouldBe(new SearchResultResponse(DocumentSource.Review, sourceId, "Vazou tudo.", 0.8765));
    }

    [Theory]
    [InlineData("", 5)]
    [InlineData("embalagem", 0)]
    [InlineData("embalagem", 21)]
    public void Validator_RejectsInvalidQueries(string query, int limit)
    {
        new SemanticSearchQueryValidator().Validate(new SemanticSearchQuery(query, limit)).IsValid.ShouldBeFalse();
    }
}
