using RestaurantCopilot.Domain.Reviews;

namespace RestaurantCopilot.Application.Reviews;

public sealed record ReviewResponse(
    Guid Id,
    int OrderNumber,
    Guid? MenuItemId,
    int Rating,
    string Comment,
    DateTimeOffset CreatedAt,
    ReviewClassificationResponse? Classification)
{
    public static ReviewResponse FromEntity(Review review, int orderNumber) =>
        new(
            review.Id,
            orderNumber,
            review.MenuItemId,
            review.Rating.Value,
            review.Comment,
            review.CreatedAt,
            review.Sentiment is { } sentiment ? new ReviewClassificationResponse(sentiment, review.Themes) : null);
}

/// <summary>The AI classification; null in <see cref="ReviewResponse"/> while classification is pending.</summary>
public sealed record ReviewClassificationResponse(ReviewSentiment Sentiment, IReadOnlyList<ReviewTheme> Themes);
