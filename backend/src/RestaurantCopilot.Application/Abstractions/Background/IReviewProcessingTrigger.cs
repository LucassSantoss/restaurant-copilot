namespace RestaurantCopilot.Application.Abstractions.Background;

/// <summary>
/// Wakes the background processing of reviews (classification and search indexing) right away,
/// instead of waiting for its next periodic check.
/// </summary>
public interface IReviewProcessingTrigger
{
    void Notify();
}
