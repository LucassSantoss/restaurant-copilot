using FluentValidation;
using RestaurantCopilot.Domain.Reviews;

namespace RestaurantCopilot.Application.Reviews.CreateReview;

internal sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(command => command.OrderNumber).GreaterThan(0);
        RuleFor(command => command.Rating).InclusiveBetween(Rating.Min, Rating.Max);
        RuleFor(command => command.Comment).NotEmpty().MaximumLength(Review.CommentMaxLength);
        RuleFor(command => command.MenuItemId).NotEqual(Guid.Empty);
    }
}
