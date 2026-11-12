using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Application.Reviews.Classification;
using RestaurantCopilot.Application.Tests.TestDoubles;
using RestaurantCopilot.Domain.Common;
using RestaurantCopilot.Domain.Reviews;

namespace RestaurantCopilot.Application.Tests.Reviews;

public sealed class LlmReviewClassifierTests
{
    private readonly ILlmClient _llm = Substitute.For<ILlmClient>();

    [Fact]
    public async Task ClassifyAsync_MapsIndexesBackToReviewsAndIgnoresUnknownIndexes()
    {
        var late = TestData.Review("Demorou demais.");
        var tasty = TestData.Review("Sabor incrível!", rating: 5);
        LlmRequest? sent = null;
        _llm.GenerateAsync(Arg.Do<LlmRequest>(request => sent = request), Arg.Any<CancellationToken>())
            .Returns(Respond("""
                { "reviews": [
                  { "index": 0, "sentiment": "Negative", "themes": ["Delay"] },
                  { "index": 1, "sentiment": "Positive", "themes": ["Taste"] },
                  { "index": 7, "sentiment": "Positive", "themes": [] }
                ] }
                """));

        var result = await new LlmReviewClassifier(_llm).ClassifyAsync([late, tasty], TestContext.Current.CancellationToken);

        result.Value.Count.ShouldBe(2);
        result.Value[late.Id].Sentiment.ShouldBe(ReviewSentiment.Negative);
        result.Value[late.Id].Themes.ShouldBe([ReviewTheme.Delay]);
        result.Value[tasty.Id].Sentiment.ShouldBe(ReviewSentiment.Positive);
        sent.ShouldNotBeNull();
        sent.SystemInstruction.ShouldBe(LlmReviewClassifier.SystemInstruction);
        sent.Messages.OfType<UserMessage>().Single().Text.ShouldContain("Demorou demais.");
    }

    [Fact]
    public async Task ClassifyAsync_WithNoReviews_DoesNotCallTheLlm()
    {
        var result = await new LlmReviewClassifier(_llm).ClassifyAsync([], TestContext.Current.CancellationToken);

        result.Value.ShouldBeEmpty();
        await _llm.DidNotReceiveWithAnyArgs().GenerateAsync(default!, TestContext.Current.CancellationToken);
    }

    private static Result<LlmResponse> Respond(string json) => new LlmResponse(json, [], LlmUsage.None);
}
