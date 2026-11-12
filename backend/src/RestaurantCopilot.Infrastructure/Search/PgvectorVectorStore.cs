using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using RestaurantCopilot.Application.Abstractions.Search;
using RestaurantCopilot.Infrastructure.Persistence;

namespace RestaurantCopilot.Infrastructure.Search;

/// <summary><see cref="IVectorStore"/> on PostgreSQL + pgvector, next to the relational data.</summary>
internal sealed class PgvectorVectorStore(ApplicationDbContext dbContext, TimeProvider timeProvider) : IVectorStore
{
    public int Dimensions => SearchDocument.Dimensions;

    public async Task ReplaceAllAsync(IReadOnlyList<VectorDocument> documents, CancellationToken cancellationToken)
    {
        var indexedAt = timeProvider.GetUtcNow();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Set<SearchDocument>().ExecuteDeleteAsync(cancellationToken);
        dbContext.Set<SearchDocument>().AddRange(documents.Select(document => new SearchDocument
        {
            Id = Guid.CreateVersion7(),
            Source = document.Source,
            SourceId = document.SourceId,
            Content = document.Content,
            Embedding = new Vector(document.Embedding),
            IndexedAt = indexedAt,
        }));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        dbContext.ChangeTracker.Clear();
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryVector,
        int limit,
        DocumentSource? source,
        CancellationToken cancellationToken)
    {
        var vector = new Vector(queryVector);
        var documents = dbContext.Set<SearchDocument>().AsNoTracking();
        if (source is not null)
        {
            documents = documents.Where(document => document.Source == source);
        }

        return await documents
            .OrderBy(document => document.Embedding.CosineDistance(vector))
            .Take(limit)
            .Select(document => new VectorSearchResult(
                document.Source,
                document.SourceId,
                document.Content,
                1 - document.Embedding.CosineDistance(vector)))
            .ToListAsync(cancellationToken);
    }
}
