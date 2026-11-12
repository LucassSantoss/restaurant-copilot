using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace RestaurantCopilot.Infrastructure.Ai.Gemini;

/// <summary>
/// Process-wide limiter for embedded texts per minute, shared by every embedding call, so a reindex
/// and concurrent searches together stay under the provider quota.
/// </summary>
internal sealed class GeminiEmbeddingRateLimiter(RateLimiter limiter) : IAsyncDisposable
{
    public GeminiEmbeddingRateLimiter(IOptions<GeminiOptions> options)
        : this(new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = options.Value.EmbeddingRequestsPerMinute,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,
            QueueLimit = int.MaxValue,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        }))
    {
    }

    /// <summary>Waits until <paramref name="texts"/> texts may be embedded.</summary>
    public async ValueTask<RateLimitLease> AcquireAsync(int texts, CancellationToken cancellationToken) =>
        await limiter.AcquireAsync(texts, cancellationToken);

    public ValueTask DisposeAsync() => limiter.DisposeAsync();
}
