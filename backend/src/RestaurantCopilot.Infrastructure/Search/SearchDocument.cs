using Pgvector;
using RestaurantCopilot.Application.Abstractions.Search;

namespace RestaurantCopilot.Infrastructure.Search;

/// <summary>
/// Persistence model of an embedded document. It belongs to the search index, not to the domain:
/// it is derived data that can always be rebuilt from menu items and reviews.
/// </summary>
internal sealed class SearchDocument
{
    /// <summary>Vector size of the <c>embedding</c> column. Changing it requires a migration.</summary>
    public const int Dimensions = 768;

    public Guid Id { get; init; }

    public DocumentSource Source { get; init; }

    public Guid SourceId { get; init; }

    public required string Content { get; init; }

    public required Vector Embedding { get; init; }

    public DateTimeOffset IndexedAt { get; init; }
}
