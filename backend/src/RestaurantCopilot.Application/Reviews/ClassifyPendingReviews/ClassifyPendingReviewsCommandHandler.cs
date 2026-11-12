using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Reviews.Classification;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Reviews.ClassifyPendingReviews;

internal sealed class ClassifyPendingReviewsCommandHandler(
    IReviewRepository reviews,
    IReviewClassifier classifier,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<ClassifyPendingReviewsCommand, ClassifyPendingReviewsResponse>
{
    /// <summary>Reviews per LLM call: few calls for the free tier, small enough for reliable output.</summary>
    internal const int BatchSize = 25;

    public async Task<Result<ClassifyPendingReviewsResponse>> HandleAsync(
        ClassifyPendingReviewsCommand command,
        CancellationToken cancellationToken)
    {
        var classifiedCount = 0;
        string? stoppedReason = null;

        while (await reviews.ListUnclassifiedAsync(BatchSize, cancellationToken) is { Count: > 0 } batch)
        {
            var result = await classifier.ClassifyAsync(batch, cancellationToken);
            if (result.IsFailure)
            {
                stoppedReason = result.Error.Message;
                break;
            }

            var now = timeProvider.GetUtcNow();
            var classifiedInBatch = batch
                .Where(review => result.Value.TryGetValue(review.Id, out var classification)
                    && review.Classify(classification.Sentiment, classification.Themes, now).IsSuccess)
                .Count();

            await unitOfWork.SaveChangesAsync(cancellationToken);
            classifiedCount += classifiedInBatch;

            // The model ignored the whole batch: retrying the same batch would loop forever.
            if (classifiedInBatch == 0)
            {
                stoppedReason = "The model did not classify any review of the last batch.";
                break;
            }
        }

        var pending = await reviews.CountUnclassifiedAsync(cancellationToken);
        return new ClassifyPendingReviewsResponse(classifiedCount, pending, stoppedReason);
    }
}
