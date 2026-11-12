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
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("pgvector/pgvector:pg18").Build();

    public static string SeedDirectory { get; } = FindSeedDirectory();

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

    private static string FindSeedDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "seed");
            if (File.Exists(Path.Combine(candidate, "menu.json")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("Could not find the repository 'seed' directory.");
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresTestCollectionDefinition : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
