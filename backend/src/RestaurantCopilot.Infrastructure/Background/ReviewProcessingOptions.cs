using System.ComponentModel.DataAnnotations;

namespace RestaurantCopilot.Infrastructure.Background;

/// <summary>Background processing of reviews, bound from the "ReviewProcessing" configuration section.</summary>
public sealed class ReviewProcessingOptions
{
    public const string SectionName = "ReviewProcessing";

    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Also classify pending reviews with the LLM. Each new review costs one generation request, which
    /// counts against the free-tier daily quota; indexing only uses the separate embeddings quota.
    /// </summary>
    public bool ClassifyReviews { get; init; } = true;

    /// <summary>How often to look for pending work when nobody triggers the worker.</summary>
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Upper bound of the wait after consecutive failures (the wait doubles on each failure).</summary>
    public TimeSpan MaxBackoff { get; init; } = TimeSpan.FromMinutes(15);
}
