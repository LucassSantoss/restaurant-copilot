using RestaurantCopilot.Domain.Reviews;

namespace RestaurantCopilot.Application.Abstractions.Persistence;

public interface IReviewRepository
{
    void Add(Review review);

    /// <summary>Oldest reviews that have not been classified yet, tracked for update.</summary>
    Task<IReadOnlyList<Review>> ListUnclassifiedAsync(int limit, CancellationToken cancellationToken);

    Task<int> CountUnclassifiedAsync(CancellationToken cancellationToken);

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
