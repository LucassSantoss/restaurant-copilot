using Microsoft.EntityFrameworkCore;

namespace RestaurantCopilot.Infrastructure.Persistence;

internal static class PersistenceConfiguration
{
    /// <summary>
    /// Shared provider setup for runtime and design time. Tables and columns use snake_case,
    /// the PostgreSQL convention, which also keeps the schema natural for LLM-generated SQL.
    /// </summary>
    public static void Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql => npgsql.UseVector())
            .UseSnakeCaseNamingConvention();
}
