using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Abstractions.Search;
using RestaurantCopilot.Application.Chat;
using RestaurantCopilot.Application.Common;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Search.IndexPendingDocuments;

internal sealed class IndexPendingDocumentsCommandHandler(
    IRestaurantRepository restaurants,
    IMenuItemRepository menuItems,
    IReviewRepository reviews,
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    IChatCache chatCache) : ICommandHandler<IndexPendingDocumentsCommand, IndexPendingDocumentsResponse>
{
    /// <summary>Reviews per embedding batch; small enough to make steady progress under the free-tier quota.</summary>
    internal const int BatchSize = 50;

    public async Task<Result<IndexPendingDocumentsResponse>> HandleAsync(
        IndexPendingDocumentsCommand command,
        CancellationToken cancellationToken)
    {
        if (embeddingService.Dimensions != vectorStore.Dimensions)
        {
            return SearchErrors.DimensionMismatch(embeddingService.Dimensions, vectorStore.Dimensions);
        }

        var restaurant = await restaurants.GetAsync(cancellationToken);
        if (restaurant is null)
        {
            return ApplicationErrors.RestaurantNotConfigured;
        }

        var indexed = 0;
        var pendingMenu = (await menuItems.ListNotIndexedAsync(cancellationToken))
            .Select(item => (Source: DocumentSource.MenuItem, item.Id, Content: SearchDocumentBuilder.ForMenuItem(item)))
            .ToList();

        var stoppedReason = await IndexAsync(pendingMenu, cancellationToken);
        indexed += stoppedReason is null ? pendingMenu.Count : 0;

        var seen = new HashSet<Guid>();
        while (stoppedReason is null
            && await reviews.ListNotIndexedAsync(BatchSize, cancellationToken) is { Count: > 0 } batch)
        {
            // A review that is still pending after being indexed means the index did not persist it;
            // stop instead of spending embedding quota on the same batch forever.
            if (!batch.All(review => seen.Add(review.Id)))
            {
                stoppedReason = "Indexed reviews are still reported as pending.";
                break;
            }

            var documents = batch
                .Select(review => (Source: DocumentSource.Review, review.Id, Content: SearchDocumentBuilder.ForReview(review, restaurant)))
                .ToList();
            stoppedReason = await IndexAsync(documents, cancellationToken);
            indexed += stoppedReason is null ? documents.Count : 0;
        }

        if (indexed > 0)
        {
            await chatCache.InvalidateAsync(cancellationToken);
        }

        return new IndexPendingDocumentsResponse(indexed, stoppedReason);
    }

    /// <summary>Embeds and stores the documents; returns why it failed, or null on success.</summary>
    private async Task<string?> IndexAsync(
        List<(DocumentSource Source, Guid Id, string Content)> pending,
        CancellationToken cancellationToken)
    {
        if (pending.Count == 0)
        {
            return null;
        }

        var embeddings = await embeddingService.EmbedAsync(
            pending.Select(document => document.Content).ToList(), EmbeddingPurpose.Document, cancellationToken);
        if (embeddings.IsFailure)
        {
            return embeddings.Error.Message;
        }

        var documents = pending
            .Zip(embeddings.Value, (document, embedding) => new VectorDocument(document.Source, document.Id, document.Content, embedding))
            .ToList();
        await vectorStore.UpsertAsync(documents, cancellationToken);
        return null;
    }
}
