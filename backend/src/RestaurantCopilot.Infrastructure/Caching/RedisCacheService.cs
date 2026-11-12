using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using RestaurantCopilot.Application.Abstractions.Caching;
using StackExchange.Redis;

namespace RestaurantCopilot.Infrastructure.Caching;

/// <summary>
/// <see cref="ICacheService"/> over Redis (through <see cref="IDistributedCache"/>), storing JSON.
/// Redis failures are logged and swallowed: the cache only saves time and quota, so its absence
/// must never turn into an error for the user.
/// </summary>
internal sealed class RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger) : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            var bytes = await cache.GetAsync(key, cancellationToken);
            return bytes is null ? null : JsonSerializer.Deserialize<T>(bytes, SerializerOptions);
        }
        catch (Exception exception) when (exception is RedisException or JsonException)
        {
            logger.LogWarning(exception, "Cache read failed for {Key}; treating it as a miss", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan timeToLive, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);
            await cache.SetAsync(key, bytes, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = timeToLive }, cancellationToken);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Cache write failed for {Key}; continuing without cache", key);
        }
    }
}
