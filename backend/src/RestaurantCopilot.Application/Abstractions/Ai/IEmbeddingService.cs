using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Abstractions.Ai;

public interface IEmbeddingService
{
    /// <summary>Vector size produced by the configured model; the vector store schema depends on it.</summary>
    int Dimensions { get; }

    /// <summary>Embeds the texts, returning one vector per text in the same order.</summary>
    Task<Result<IReadOnlyList<float[]>>> EmbedAsync(
        IReadOnlyList<string> texts,
        EmbeddingPurpose purpose,
        CancellationToken cancellationToken);
}

/// <summary>
/// Retrieval models embed stored documents and search queries slightly differently;
/// telling them which is which improves search quality.
/// </summary>
public enum EmbeddingPurpose
{
    Document,
    Query,
}
