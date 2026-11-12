using RestaurantCopilot.Application.Reviews.CreateReview;

namespace RestaurantCopilot.Application.Tests.Reviews;

public sealed class CreateReviewCommandValidatorTests
{
    private readonly CreateReviewCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        _validator.Validate(new CreateReviewCommand(1001, null, 5, "Ótimo!")).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_InvalidCommand_ReportsEveryField()
    {
        var result = _validator.Validate(new CreateReviewCommand(0, Guid.Empty, 6, ""));

        result.Errors.Select(error => error.PropertyName).Distinct()
            .ShouldBe(["OrderNumber", "Rating", "Comment", "MenuItemId"], ignoreOrder: true);
    }
}
