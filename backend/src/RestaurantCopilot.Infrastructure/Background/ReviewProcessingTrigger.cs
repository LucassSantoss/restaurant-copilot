using RestaurantCopilot.Application.Abstractions.Background;

namespace RestaurantCopilot.Infrastructure.Background;

/// <summary>
/// Single-slot signal: many notifications before the worker wakes up collapse into one run, which
/// then processes everything that is pending.
/// </summary>
internal sealed class ReviewProcessingTrigger : IReviewProcessingTrigger, IDisposable
{
    private readonly SemaphoreSlim _signal = new(0, 1);

    public void Notify()
    {
        try
        {
            _signal.Release();
        }
        catch (SemaphoreFullException)
        {
            // A run is already scheduled; it will pick this work up too.
        }
    }

    /// <summary>Waits for a notification or for the timeout; true when it was notified.</summary>
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
        _signal.WaitAsync(timeout, cancellationToken);

    public void Dispose() => _signal.Dispose();
}
