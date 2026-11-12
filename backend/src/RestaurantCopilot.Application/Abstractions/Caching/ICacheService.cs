namespace RestaurantCopilot.Application.Abstractions.Caching;

/// <summary>
/// Key-value cache for derived data. Implementations must degrade gracefully: if the cache is
/// down, reads behave as misses and writes are skipped, so a cache outage never fails a request.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
        where T : class;

    Task SetAsync<T>(string key, T value, TimeSpan timeToLive, CancellationToken cancellationToken)
        where T : class;
}
