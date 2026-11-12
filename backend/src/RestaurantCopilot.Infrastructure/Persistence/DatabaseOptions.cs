namespace RestaurantCopilot.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Applies pending EF Core migrations when the API starts. Meant for local development.</summary>
    public bool ApplyMigrationsOnStartup { get; init; }

    /// <summary>
    /// Directory with the seed JSON files, absolute or relative to the content root. When set,
    /// an empty database is populated on startup; when empty, seeding is skipped.
    /// </summary>
    public string? SeedPath { get; init; }
}
