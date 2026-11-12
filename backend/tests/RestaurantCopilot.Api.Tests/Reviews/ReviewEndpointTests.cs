using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RestaurantCopilot.Api.Tests.Infrastructure;
using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Api.Tests.Reviews;

[Collection(ApiTestCollectionDefinition.Name)]
public sealed class ReviewEndpointTests(ApiFactory factory)
{
    [Fact]
    public async Task Post_ClassifiesReviewWithLlm()
    {
        var llm = Substitute.For<ILlmClient>();
        llm.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>()).Returns(Result.Success(new LlmResponse(
            """{ "reviews": [{ "index": 0, "sentiment": "Negative", "themes": ["Packaging"] }] }""", [], LlmUsage.None)));
        await using var app = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton(llm)));
        using var client = app.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/reviews",
            new { orderNumber = 1001, rating = 1, comment = "A embalagem abriu e vazou tudo." },
            TestContext.Current.CancellationToken);
        var review = await response.Content.ReadFromJsonAsync<ReviewBody>(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        review.ShouldNotBeNull();
        review.OrderNumber.ShouldBe(1001);
        review.Classification.ShouldNotBeNull();
        review.Classification.Sentiment.ShouldBe("Negative");
        review.Classification.Themes.ShouldBe(["Packaging"]);
    }

    [Fact]
    public async Task Post_WithoutLlmKey_SavesPendingReview()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/reviews",
            new { orderNumber = 1002, rating = 5, comment = "Perfeito!" },
            TestContext.Current.CancellationToken);
        var review = await response.Content.ReadFromJsonAsync<ReviewBody>(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        review.ShouldNotBeNull();
        review.Classification.ShouldBeNull();
    }

    [Fact]
    public async Task Post_WithInvalidBody_ReturnsValidationProblem()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/reviews",
            new { orderNumber = 1001, rating = 7, comment = "" },
            TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemBody>(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problem.ShouldNotBeNull();
        problem.Errors.Keys.ShouldBe(["Rating", "Comment"], ignoreOrder: true);
    }

    [Fact]
    public async Task Post_ForUnknownOrder_ReturnsNotFound()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/reviews",
            new { orderNumber = 999999, rating = 5, comment = "Ótimo" },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetInsights_ForSeedPeriod_SummarizesReviews()
    {
        using var client = factory.CreateClient();

        var insights = await client.GetFromJsonAsync<InsightsBody>(
            "/api/reviews/insights?from=2026-10-01&to=2026-10-31", TestContext.Current.CancellationToken);

        insights.ShouldNotBeNull();
        insights.From.ShouldBe(new DateOnly(2026, 10, 1));
        insights.TotalReviews.ShouldBeGreaterThan(0);
        insights.AverageRating.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetInsights_WithInvertedPeriod_ReturnsBadRequest()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/reviews/insights?from=2026-10-31&to=2026-10-01", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ClassifyPending_WithoutLlmKey_ReportsWhyItStopped()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/ingestion/classify-reviews", null, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ClassifyBody>(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body.Classified.ShouldBe(0);
        body.Pending.ShouldBeGreaterThan(0);
        body.StoppedReason.ShouldNotBeNull();
    }

    private sealed record ReviewBody(Guid Id, int OrderNumber, int Rating, ClassificationBody? Classification);

    private sealed record ClassificationBody(string Sentiment, List<string> Themes);

    private sealed record ValidationProblemBody(Dictionary<string, string[]> Errors);

    private sealed record InsightsBody(DateOnly From, DateOnly To, int TotalReviews, double? AverageRating);

    private sealed record ClassifyBody(int Classified, int Pending, string? StoppedReason);
}
