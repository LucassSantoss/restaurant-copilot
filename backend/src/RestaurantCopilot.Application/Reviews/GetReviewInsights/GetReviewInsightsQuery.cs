using RestaurantCopilot.Application.Abstractions.Messaging;

namespace RestaurantCopilot.Application.Reviews.GetReviewInsights;

/// <summary>Sentiment and theme summary of the reviews written between two dates (inclusive).</summary>
/// <remarks>Without dates, covers the last <see cref="DefaultDays"/> days.</remarks>
public sealed record GetReviewInsightsQuery(DateOnly? From = null, DateOnly? To = null) : IQuery<ReviewInsightsResponse>
{
    public const int DefaultDays = 30;
    public const int MaxDays = 366;
}
