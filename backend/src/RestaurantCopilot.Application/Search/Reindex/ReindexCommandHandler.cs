using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Abstractions.Search;
using RestaurantCopilot.Application.Common;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Search.Reindex;

internal sealed class ReindexCommandHandler(
    IRestaurantRepository restaurants,
    IMenuItemRepository menuItems,
    IReviewRepository reviews,
    IEmbeddingService embeddingService,
    IVectorStore vectorStore) : ICommandHandler<ReindexCommand, ReindexResponse>
{
    public async Task<Result<ReindexResponse>> HandleAsync(ReindexCommand command, CancellationToken cancellationToken)
    {
        // Checked before spending any embedding quota: a mismatch would only fail on insert.
        if (embeddingService.Dimensions != vectorStore.Dimensions)
        {
            return SearchErrors.DimensionMismatch(embeddingService.Dimensions, vectorStore.Dimensions);
        }

        var restaurant = await restaurants.GetAsync(cancellationToken);
        if (restaurant is null)
        {
            return ApplicationErrors.RestaurantNotConfigured;
        }

        var menu = await menuItems.ListAsync(category: null, availableOnly: false, cancellationToken);
        var reviewRows = await reviews.ListForIndexingAsync(cancellationToken);

        var sources = menu
            .Select(item => (Source: DocumentSource.MenuItem, item.Id, Content: SearchDocumentBuilder.ForMenuItem(item)))
            .Concat(reviewRows.Select(review => (Source: DocumentSource.Review, review.Id, Content: SearchDocumentBuilder.ForReview(review, restaurant))))
            .ToList();

        var embeddings = await embeddingService.EmbedAsync(
            sources.Select(source => source.Content).ToList(), EmbeddingPurpose.Document, cancellationToken);
        if (embeddings.IsFailure)
        {
            return embeddings.Error;
        }

        var documents = sources
            .Zip(embeddings.Value, (source, embedding) => new VectorDocument(source.Source, source.Id, source.Content, embedding))
            .ToList();
        await vectorStore.ReplaceAllAsync(documents, cancellationToken);

        return new ReindexResponse(menu.Count, reviewRows.Count);
    }
}
