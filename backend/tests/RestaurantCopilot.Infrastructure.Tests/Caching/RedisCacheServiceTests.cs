using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RestaurantCopilot.Infrastructure.Caching;
using Testcontainers.Redis;

namespace RestaurantCopilot.Infrastructure.Tests.Caching;

public sealed class RedisCacheServiceTests : IAsyncLifetime
{
    private readonly RedisContainer _redis = new RedisBuilder("redis:8-alpine").Build();

    public async ValueTask InitializeAsync() => await _redis.StartAsync();

    public ValueTask DisposeAsync() => _redis.DisposeAsync();

    [Fact]
    public async Task SetThenGet_RoundTripsTheValue()
    {
        var cache = CreateCache(_redis.GetConnectionString());

        await cache.SetAsync("answer", new Sample("R$ 100", 3), TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);

        (await cache.GetAsync<Sample>("answer", TestContext.Current.CancellationToken)).ShouldBe(new Sample("R$ 100", 3));
    }

    [Fact]
    public async Task GetAsync_AfterTimeToLive_Misses()
    {
        var cache = CreateCache(_redis.GetConnectionString());

        await cache.SetAsync("short-lived", new Sample("x", 1), TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(600), TestContext.Current.CancellationToken);

        (await cache.GetAsync<Sample>("short-lived", TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task WhenRedisIsUnreachable_ReadsMissAndWritesDoNotThrow()
    {
        var cache = CreateCache("localhost:1,connectTimeout=200,abortConnect=true");

        await cache.SetAsync("answer", new Sample("x", 1), TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);

        (await cache.GetAsync<Sample>("answer", TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    private static RedisCacheService CreateCache(string connectionString) =>
        new(
            new RedisCache(Options.Create(new RedisCacheOptions { Configuration = connectionString, InstanceName = "test:" })),
            NullLogger<RedisCacheService>.Instance);

    private sealed record Sample(string Text, int Count);
}
