using RestaurantCopilot.Domain.Reviews;

namespace RestaurantCopilot.Application.Reviews.GetReviewInsights;

/// <param name="TotalReviews">Reviews written in the period.</param>
/// <param name="ClassifiedReviews">How many of them are classified; sentiment and themes only count these.</param>
public sealed record ReviewInsightsResponse(
    DateOnly From,
    DateOnly To,
    int TotalReviews,
    int ClassifiedReviews,
    double? AverageRating,
    SentimentBreakdown Sentiment,
    IReadOnlyList<ThemeInsight> Themes,
    IReadOnlyList<DishComplaints> MostComplainedDishes);

public sealed record SentimentBreakdown(int Positive, int Neutral, int Negative);

/// <param name="Mentions">Classified reviews that mention the theme.</param>
/// <param name="Negative">Of those, how many are negative.</param>
public sealed record ThemeInsight(ReviewTheme Theme, int Mentions, int Negative);

public sealed record DishComplaints(Guid MenuItemId, string Name, int NegativeReviews);
