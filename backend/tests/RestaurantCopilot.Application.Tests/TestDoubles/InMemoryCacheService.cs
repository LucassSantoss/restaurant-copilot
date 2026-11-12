using System.Text.Json;
using RestaurantCopilot.Application.Abstractions.Caching;

namespace RestaurantCopilot.Application.Tests.TestDoubles;

/// <summary>Dictionary-backed cache that round-trips values through JSON, like the Redis implementation.</summary>
internal sealed class InMemoryCacheService : ICacheService
{
    private readonly Dictionary<string, string> _entries = [];

    public IReadOnlyCollection<string> Keys => _entries.Keys;

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
        where T : class =>
        Task.FromResult(_entries.TryGetValue(key, out var json) ? JsonSerializer.Deserialize<T>(json) : null);

    public Task SetAsync<T>(string key, T value, TimeSpan timeToLive, CancellationToken cancellationToken)
        where T : class
    {
        _entries[key] = JsonSerializer.Serialize(value);
        return Task.CompletedTask;
    }
}
