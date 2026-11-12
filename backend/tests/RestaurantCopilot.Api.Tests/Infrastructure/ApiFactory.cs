using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace RestaurantCopilot.Api.Tests.Infrastructure;

/// <summary>
/// Boots the real API in memory against real PostgreSQL (with pgvector) and Redis containers,
/// so endpoint tests exercise the same stack that runs in production.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("pgvector/pgvector:pg18").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:8-alpine").Build();

    public async ValueTask InitializeAsync() =>
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());

        // Tests never call the real Gemini API, even if a key exists in user-secrets.
        builder.UseSetting("Gemini:ApiKey", "");
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiTestCollectionDefinition : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
