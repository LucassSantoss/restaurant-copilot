using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Common;
using RestaurantCopilot.Domain.Common;
using RestaurantCopilot.Domain.Reviews;

namespace RestaurantCopilot.Application.Reviews.GetReviewInsights;

/// <summary>
/// Aggregates the period's reviews in memory. A restaurant gets at most a few thousand reviews per
/// period, so loading a slim projection keeps the business rules in plain, testable C#; aggregating
/// in SQL would be the next step at a much larger scale.
/// </summary>
internal sealed class GetReviewInsightsQueryHandler(
    IRestaurantRepository restaurants,
    IReviewRepository reviews,
    TimeProvider timeProvider) : IQueryHandler<GetReviewInsightsQuery, ReviewInsightsResponse>
{
    internal const int MostComplainedDishesLimit = 5;

    public async Task<Result<ReviewInsightsResponse>> HandleAsync(GetReviewInsightsQuery query, CancellationToken cancellationToken)
    {
        var restaurant = await restaurants.GetAsync(cancellationToken);
        if (restaurant is null)
        {
            return ApplicationErrors.RestaurantNotConfigured;
        }

        var period = query is { From: { } from, To: { } to }
            ? new ReportingPeriod(from, to)
            : ReportingPeriod.LastDays(GetReviewInsightsQuery.DefaultDays, restaurant, timeProvider);
        var (startUtc, endUtc) = period.ToUtcRange(restaurant);

        var rows = await reviews.ListForInsightsAsync(startUtc, endUtc, cancellationToken);
        return Summarize(period, rows);
    }

    internal static ReviewInsightsResponse Summarize(ReportingPeriod period, IReadOnlyList<ReviewInsightRow> rows)
    {
        var classified = rows.Where(row => row.Sentiment is not null).ToList();

        var themes = Enum.GetValues<ReviewTheme>()
            .Select(theme => new ThemeInsight(
                theme,
                classified.Count(row => row.Themes.Contains(theme)),
                classified.Count(row => row.Themes.Contains(theme) && row.Sentiment == ReviewSentiment.Negative)))
            .Where(insight => insight.Mentions > 0)
            .OrderByDescending(insight => insight.Mentions)
            .ThenBy(insight => insight.Theme)
            .ToList();

        var mostComplainedDishes = classified
            .Where(row => row.Sentiment == ReviewSentiment.Negative && row.MenuItemId is not null)
            .GroupBy(row => (Id: row.MenuItemId!.Value, Name: row.MenuItemName!))
            .Select(group => new DishComplaints(group.Key.Id, group.Key.Name, group.Count()))
            .OrderByDescending(dish => dish.NegativeReviews)
            .ThenBy(dish => dish.Name, StringComparer.Ordinal)
            .Take(MostComplainedDishesLimit)
            .ToList();

        return new ReviewInsightsResponse(
            period.From,
            period.To,
            rows.Count,
            classified.Count,
            rows.Count == 0 ? null : Math.Round(rows.Average(row => row.Rating), 2),
            new SentimentBreakdown(
                classified.Count(row => row.Sentiment == ReviewSentiment.Positive),
                classified.Count(row => row.Sentiment == ReviewSentiment.Neutral),
                classified.Count(row => row.Sentiment == ReviewSentiment.Negative)),
            themes,
            mostComplainedDishes);
    }
}
