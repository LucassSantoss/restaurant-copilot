using RestaurantCopilot.Application.Abstractions.Messaging;

namespace RestaurantCopilot.Application.Reviews.CreateReview;

public sealed record CreateReviewCommand(int OrderNumber, Guid? MenuItemId, int Rating, string Comment)
    : ICommand<ReviewResponse>;
