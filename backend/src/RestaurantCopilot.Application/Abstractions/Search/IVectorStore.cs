namespace RestaurantCopilot.Application.Abstractions.Search;

/// <summary>Stores embedded documents and finds the ones most similar to a query vector.</summary>
public interface IVectorStore
{
    /// <summary>Vector size the store was created with; embeddings must match it.</summary>
    int Dimensions { get; }

    /// <summary>Atomically replaces the whole index, so searches never see a half-built index.</summary>
    Task ReplaceAllAsync(IReadOnlyList<VectorDocument> documents, CancellationToken cancellationToken);

    /// <summary>Adds the documents, replacing any already indexed for the same source item.</summary>
    Task UpsertAsync(IReadOnlyList<VectorDocument> documents, CancellationToken cancellationToken);

    /// <summary>The <paramref name="limit"/> nearest documents by cosine similarity, most similar first.</summary>
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryVector,
        int limit,
        DocumentSource? source,
        CancellationToken cancellationToken);
}

public enum DocumentSource
{
    MenuItem,
    Review,
}

public sealed record VectorDocument(DocumentSource Source, Guid SourceId, string Content, float[] Embedding);

/// <param name="Similarity">Cosine similarity, from -1 to 1 (higher is more similar).</param>
public sealed record VectorSearchResult(DocumentSource Source, Guid SourceId, string Content, double Similarity);
