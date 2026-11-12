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
}
