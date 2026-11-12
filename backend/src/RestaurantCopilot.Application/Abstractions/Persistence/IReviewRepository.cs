using RestaurantCopilot.Domain.Reviews;

namespace RestaurantCopilot.Application.Abstractions.Persistence;

public interface IReviewRepository
{
    void Add(Review review);

    /// <summary>Oldest reviews that have not been classified yet, tracked for update.</summary>
    Task<IReadOnlyList<Review>> ListUnclassifiedAsync(int limit, CancellationToken cancellationToken);

    Task<int> CountUnclassifiedAsync(CancellationToken cancellationToken);

    /// <summary>Reviews matching every set filter, newest first, plus how many match in total.</summary>
    Task<(IReadOnlyList<ReviewListRow> Reviews, int TotalCount)> SearchAsync(
        ReviewSearchFilter filter,
        CancellationToken cancellationToken);

    /// <summary>Oldest reviews that have no search document yet, for incremental indexing.</summary>
    Task<IReadOnlyList<ReviewDocumentRow>> ListNotIndexedAsync(int limit, CancellationToken cancellationToken);

    /// <summary>Every review, with the reviewed dish name, for building the search index.</summary>
    Task<IReadOnlyList<ReviewDocumentRow>> ListForIndexingAsync(CancellationToken cancellationToken);

    /// <summary>Lightweight projection of the reviews created in [startUtc, endUtc).</summary>
    Task<IReadOnlyList<ReviewInsightRow>> ListForInsightsAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken);
}

public sealed record ReviewInsightRow(
    int Rating,
    ReviewSentiment? Sentiment,
    IReadOnlyList<ReviewTheme> Themes,
    Guid? MenuItemId,
    string? MenuItemName);

public sealed record ReviewDocumentRow(Guid Id, int Rating, string Comment, DateTimeOffset CreatedAt, string? MenuItemName);

/// <summary>Null properties do not filter. The date range is [StartUtc, EndUtc).</summary>
public sealed record ReviewSearchFilter(
    DateTimeOffset? StartUtc,
    DateTimeOffset? EndUtc,
    ReviewSentiment? Sentiment,
    ReviewTheme? Theme,
    Guid? MenuItemId,
    int? MaxRating,
    int Limit);

public sealed record ReviewListRow(
    Guid Id,
    int OrderNumber,
    DateTimeOffset CreatedAt,
    int Rating,
    string Comment,
    string? MenuItemName,
    ReviewSentiment? Sentiment,
    IReadOnlyList<ReviewTheme> Themes);
