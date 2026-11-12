using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Common;
using RestaurantCopilot.Application.Reviews.GetReviewInsights;
using RestaurantCopilot.Application.Tests.TestDoubles;
using RestaurantCopilot.Domain.Reviews;

namespace RestaurantCopilot.Application.Tests.Reviews;

public sealed class GetReviewInsightsQueryHandlerTests
{
    private static readonly ReportingPeriod October = new(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));
    private static readonly Guid LasagnaId = Guid.NewGuid();
    private static readonly Guid PizzaId = Guid.NewGuid();

    private readonly IRestaurantRepository _restaurants = Substitute.For<IRestaurantRepository>();
    private readonly IReviewRepository _reviews = Substitute.For<IReviewRepository>();

    [Fact]
    public void Summarize_CountsSentimentThemesAndComplainedDishes()
    {
        ReviewInsightRow[] rows =
        [
            new(1, ReviewSentiment.Negative, [ReviewTheme.Packaging], LasagnaId, "Lasanha"),
            new(2, ReviewSentiment.Negative, [ReviewTheme.Packaging, ReviewTheme.Delay], LasagnaId, "Lasanha"),
            new(1, ReviewSentiment.Negative, [ReviewTheme.Taste], PizzaId, "Pizza"),
            new(5, ReviewSentiment.Positive, [ReviewTheme.Taste], PizzaId, "Pizza"),
            new(3, ReviewSentiment.Neutral, [], null, null),
            new(4, null, [], null, null),
        ];

        var insights = GetReviewInsightsQueryHandler.Summarize(October, rows);

        insights.TotalReviews.ShouldBe(6);
        insights.ClassifiedReviews.ShouldBe(5);
        insights.AverageRating.ShouldBe(2.67);
        insights.Sentiment.ShouldBe(new SentimentBreakdown(Positive: 1, Neutral: 1, Negative: 3));
        insights.Themes.ShouldBe(
        [
            new ThemeInsight(ReviewTheme.Taste, Mentions: 2, Negative: 1),
            new ThemeInsight(ReviewTheme.Packaging, Mentions: 2, Negative: 2),
            new ThemeInsight(ReviewTheme.Delay, Mentions: 1, Negative: 1),
        ]);
        insights.MostComplainedDishes.ShouldBe(
        [
            new DishComplaints(LasagnaId, "Lasanha", 2),
            new DishComplaints(PizzaId, "Pizza", 1),
        ]);
    }

    [Fact]
    public void Summarize_WithoutReviews_ReturnsEmptySummary()
    {
        var insights = GetReviewInsightsQueryHandler.Summarize(October, []);

        insights.AverageRating.ShouldBeNull();
        insights.Themes.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithoutDates_QueriesLast30RestaurantDays()
    {
        _restaurants.GetAsync(Arg.Any<CancellationToken>()).Returns(TestData.Restaurant());
        _reviews.ListForInsightsAsync(default, default, TestContext.Current.CancellationToken).ReturnsForAnyArgs([]);
        var handler = new GetReviewInsightsQueryHandler(_restaurants, _reviews, new FixedTimeProvider(TestData.Now));

        var result = await handler.HandleAsync(new GetReviewInsightsQuery(), TestContext.Current.CancellationToken);

        result.Value.From.ShouldBe(new DateOnly(2026, 10, 13));
        result.Value.To.ShouldBe(new DateOnly(2026, 11, 11));
        await _reviews.Received(1).ListForInsightsAsync(
            new DateTimeOffset(2026, 10, 13, 3, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 12, 3, 0, 0, TimeSpan.Zero),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_BeforeSeeding_ReturnsRestaurantNotConfigured()
    {
        var handler = new GetReviewInsightsQueryHandler(_restaurants, _reviews, TimeProvider.System);

        var result = await handler.HandleAsync(new GetReviewInsightsQuery(), TestContext.Current.CancellationToken);

        result.Error.ShouldBe(ApplicationErrors.RestaurantNotConfigured);
    }

    [Theory]
    [InlineData("2026-10-31", "2026-10-01")]
    [InlineData("2025-01-01", "2026-10-01")]
    public void Validator_RejectsInvalidPeriods(string from, string to)
    {
        var query = new GetReviewInsightsQuery(DateOnly.Parse(from, System.Globalization.CultureInfo.InvariantCulture), DateOnly.Parse(to, System.Globalization.CultureInfo.InvariantCulture));

        new GetReviewInsightsQueryValidator().Validate(query).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validator_RejectsHalfOpenPeriod()
    {
        new GetReviewInsightsQueryValidator().Validate(new GetReviewInsightsQuery(new DateOnly(2026, 10, 1))).IsValid.ShouldBeFalse();
    }
}
