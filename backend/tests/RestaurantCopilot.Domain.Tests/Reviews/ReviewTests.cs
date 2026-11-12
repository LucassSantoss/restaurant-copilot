using RestaurantCopilot.Domain.Reviews;

namespace RestaurantCopilot.Domain.Tests.Reviews;

public sealed class ReviewTests
{
    private static readonly Rating FiveStars = Rating.Create(5).Value;
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 10, 9, 0, 0, TimeSpan.FromHours(-3));

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Rating_OutOfRange_Fails(int value)
    {
        Rating.Create(value).Error.Code.ShouldBe("Rating.OutOfRange");
    }

    [Fact]
    public void Create_WithValidData_TrimsCommentAndStoresUtc()
    {
        var review = Review.Create(Guid.NewGuid(), null, FiveStars, "  Chegou quentinho! ", CreatedAt).Value;

        review.Comment.ShouldBe("Chegou quentinho!");
        review.Rating.Value.ShouldBe(5);
        review.CreatedAt.Offset.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void Create_WithoutOrder_Fails()
    {
        Review.Create(Guid.Empty, null, FiveStars, "Ótimo", CreatedAt).Error.Code.ShouldBe("Review.Order");
    }

    [Fact]
    public void Create_WithEmptyMenuItemId_Fails()
    {
        Review.Create(Guid.NewGuid(), Guid.Empty, FiveStars, "Ótimo", CreatedAt).Error.Code.ShouldBe("Review.MenuItem");
    }

    [Fact]
    public void Create_WithoutComment_Fails()
    {
        Review.Create(Guid.NewGuid(), null, FiveStars, " ", CreatedAt).Error.Code.ShouldBe("Review.Comment.Required");
    }

    [Fact]
    public void Classify_StoresSentimentAndDistinctOrderedThemes()
    {
        var review = Review.Create(Guid.NewGuid(), null, FiveStars, "Atrasou e veio vazando", CreatedAt).Value;

        var result = review.Classify(
            ReviewSentiment.Negative,
            [ReviewTheme.Packaging, ReviewTheme.Delay, ReviewTheme.Packaging],
            CreatedAt.AddHours(1));

        result.IsSuccess.ShouldBeTrue();
        review.IsClassified.ShouldBeTrue();
        review.Sentiment.ShouldBe(ReviewSentiment.Negative);
        review.Themes.ShouldBe([ReviewTheme.Delay, ReviewTheme.Packaging]);
    }

    [Fact]
    public void Classify_Again_ReplacesPreviousClassification()
    {
        var review = Review.Create(Guid.NewGuid(), null, FiveStars, "Ótimo", CreatedAt).Value;
        review.Classify(ReviewSentiment.Negative, [ReviewTheme.Price], CreatedAt);

        review.Classify(ReviewSentiment.Positive, [], CreatedAt);

        review.Sentiment.ShouldBe(ReviewSentiment.Positive);
        review.Themes.ShouldBeEmpty();
    }

    [Fact]
    public void Classify_WithUndefinedTheme_FailsAndKeepsReviewUnclassified()
    {
        var review = Review.Create(Guid.NewGuid(), null, FiveStars, "Ótimo", CreatedAt).Value;

        var result = review.Classify(ReviewSentiment.Positive, [(ReviewTheme)42], CreatedAt);

        result.Error.Code.ShouldBe("Review.Classification");
        review.IsClassified.ShouldBeFalse();
    }
}
