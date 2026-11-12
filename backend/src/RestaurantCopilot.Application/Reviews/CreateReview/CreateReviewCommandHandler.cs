using Microsoft.Extensions.Logging;
using RestaurantCopilot.Application.Abstractions.Background;
using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Chat;
using RestaurantCopilot.Application.Common;
using RestaurantCopilot.Application.Reviews.Classification;
using RestaurantCopilot.Domain.Common;
using RestaurantCopilot.Domain.Reviews;

namespace RestaurantCopilot.Application.Reviews.CreateReview;

/// <summary>
/// Stores a review and classifies it right away. If the LLM is unavailable (e.g. free-tier
/// rate limit), the review is still saved and stays pending: losing customer feedback because of
/// an AI outage would be worse than a delayed insight. The background review processing then
/// classifies what is pending and adds the review to the semantic search index.
/// </summary>
internal sealed class CreateReviewCommandHandler(
    IOrderRepository orders,
    IMenuItemRepository menuItems,
    IReviewRepository reviews,
    IReviewClassifier classifier,
    IUnitOfWork unitOfWork,
    IChatCache chatCache,
    IReviewProcessingTrigger reviewProcessing,
    TimeProvider timeProvider,
    ILogger<CreateReviewCommandHandler> logger) : ICommandHandler<CreateReviewCommand, ReviewResponse>
{
    public async Task<Result<ReviewResponse>> HandleAsync(CreateReviewCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.GetByNumberAsync(command.OrderNumber, cancellationToken);
        if (order is null)
        {
            return ApplicationErrors.OrderNotFound(command.OrderNumber);
        }

        if (command.MenuItemId is { } menuItemId && !await menuItems.ExistsAsync(menuItemId, cancellationToken))
        {
            return ApplicationErrors.MenuItemNotFound(menuItemId);
        }

        var rating = Rating.Create(command.Rating);
        if (rating.IsFailure)
        {
            return rating.Error;
        }

        var review = Review.Create(order.Id, command.MenuItemId, rating.Value, command.Comment, timeProvider.GetUtcNow());
        if (review.IsFailure)
        {
            return review.Error;
        }

        await TryClassifyAsync(review.Value, cancellationToken);

        reviews.Add(review.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await chatCache.InvalidateAsync(cancellationToken);
        reviewProcessing.Notify();

        return ReviewResponse.FromEntity(review.Value, order.Number);
    }

    private async Task TryClassifyAsync(Review review, CancellationToken cancellationToken)
    {
        var result = await classifier.ClassifyAsync([review], cancellationToken);
        if (result.IsFailure)
        {
            logger.LogWarning("Review saved without classification: {ErrorCode}", result.Error.Code);
            return;
        }

        if (result.Value.TryGetValue(review.Id, out var classification))
        {
            review.Classify(classification.Sentiment, classification.Themes, timeProvider.GetUtcNow());
        }
    }
}
