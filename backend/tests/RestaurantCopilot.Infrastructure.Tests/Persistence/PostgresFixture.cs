using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RestaurantCopilot.Infrastructure.Persistence;
using RestaurantCopilot.Infrastructure.Persistence.Seeding;
using Testcontainers.PostgreSql;

namespace RestaurantCopilot.Infrastructure.Tests.Persistence;

/// <summary>
/// A real PostgreSQL (pgvector image) shared by the persistence tests, migrated and seeded
/// once with the committed seed data. Tests must not modify the seeded rows.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public const string ReadOnlyUser = "copilot_readonly";
    public const string ReadOnlyPassword = "test-readonly";

    // Same image and init script as docker-compose, so the read-only role exists exactly as in production.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("pgvector/pgvector:pg18")
        .WithResourceMapping(new FileInfo(Path.Combine(RepositoryRoot, "docker", "postgres", "initdb", "01-readonly-user.sh")), "/docker-entrypoint-initdb.d/")
        .WithEnvironment("POSTGRES_READONLY_PASSWORD", ReadOnlyPassword)
        .Build();

    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    public static string SeedDirectory { get; } = Path.Combine(RepositoryRoot, "seed");

    public string ConnectionString => _container.GetConnectionString();

    public string ReadOnlyConnectionString => new Npgsql.NpgsqlConnectionStringBuilder(ConnectionString)
    {
        Username = ReadOnlyUser,
        Password = ReadOnlyPassword,
    }.ConnectionString;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
        await new DatabaseSeeder(dbContext, NullLogger<DatabaseSeeder>.Instance).SeedAsync(SeedDirectory, CancellationToken.None);
    }

    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        PersistenceConfiguration.Configure(options, _container.GetConnectionString());
        return new ApplicationDbContext(options.Options);
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "seed", "menu.json")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not find the repository root (the directory containing 'seed').");
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresTestCollectionDefinition : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
