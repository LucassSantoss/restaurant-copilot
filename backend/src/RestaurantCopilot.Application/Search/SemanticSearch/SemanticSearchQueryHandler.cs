using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Abstractions.Search;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Search.SemanticSearch;

internal sealed class SemanticSearchQueryHandler(IEmbeddingService embeddingService, IVectorStore vectorStore)
    : IQueryHandler<SemanticSearchQuery, IReadOnlyList<SearchResultResponse>>
{
    public async Task<Result<IReadOnlyList<SearchResultResponse>>> HandleAsync(
        SemanticSearchQuery query,
        CancellationToken cancellationToken)
    {
        var embedding = await embeddingService.EmbedAsync([query.Query], EmbeddingPurpose.Query, cancellationToken);
        if (embedding.IsFailure)
        {
            return embedding.Error;
        }

        var results = await vectorStore.SearchAsync(embedding.Value[0], query.Limit, query.Source, cancellationToken);

        return results
            .Select(result => new SearchResultResponse(result.Source, result.SourceId, result.Content, Math.Round(result.Similarity, 4)))
            .ToList();
    }
}
