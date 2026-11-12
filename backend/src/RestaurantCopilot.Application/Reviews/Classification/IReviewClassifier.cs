using RestaurantCopilot.Domain.Common;
using RestaurantCopilot.Domain.Reviews;

namespace RestaurantCopilot.Application.Reviews.Classification;

public interface IReviewClassifier
{
    /// <summary>
    /// Classifies several reviews in a single LLM call (saving free-tier quota). Reviews the model
    /// skipped are simply absent from the result and stay pending.
    /// </summary>
    Task<Result<IReadOnlyDictionary<Guid, ReviewClassification>>> ClassifyAsync(
        IReadOnlyList<Review> reviews,
        CancellationToken cancellationToken);
}

public sealed record ReviewClassification(ReviewSentiment Sentiment, IReadOnlyList<ReviewTheme> Themes);
