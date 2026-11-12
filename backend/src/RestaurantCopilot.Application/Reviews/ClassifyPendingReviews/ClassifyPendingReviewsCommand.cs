using RestaurantCopilot.Application.Abstractions.Messaging;

namespace RestaurantCopilot.Application.Reviews.ClassifyPendingReviews;

/// <summary>Classifies every review that is still pending (seed data, or reviews saved during an LLM outage).</summary>
public sealed record ClassifyPendingReviewsCommand : ICommand<ClassifyPendingReviewsResponse>;

/// <param name="Classified">Reviews classified by this run.</param>
/// <param name="Pending">Reviews still pending after the run.</param>
/// <param name="StoppedReason">Why the run stopped early (e.g. rate limit), or null when it finished.</param>
public sealed record ClassifyPendingReviewsResponse(int Classified, int Pending, string? StoppedReason);
