using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace RestaurantCopilot.Api.Tests.Infrastructure;

/// <summary>
/// Boots the real API in memory against real PostgreSQL (with pgvector) and Redis containers,
/// so endpoint tests exercise the same stack that runs in production.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string ReadOnlyPassword = "test-readonly";

    // Same image and init script as docker-compose, so the read-only role exists exactly as in production.
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("pgvector/pgvector:pg18")
        .WithResourceMapping(new FileInfo(Path.Combine(FindRepositoryRoot(), "docker", "postgres", "initdb", "01-readonly-user.sh")), "/docker-entrypoint-initdb.d/")
        .WithEnvironment("POSTGRES_READONLY_PASSWORD", ReadOnlyPassword)
        .Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:8-alpine").Build();

    public async ValueTask InitializeAsync() =>
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:PostgresReadOnly", new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Username = "copilot_readonly",
            Password = ReadOnlyPassword,
        }.ConnectionString);
        builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());

        // Tests never call the real Gemini API, even if a key exists in user-secrets.
        builder.UseSetting("Gemini:ApiKey", "");

        // The background worker is enabled only by the tests about it, so it cannot interfere with others.
        builder.UseSetting("ReviewProcessing:Enabled", "false");
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "docker", "postgres", "initdb")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not find the repository root (the directory containing 'docker').");
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
