namespace RestaurantCopilot.Infrastructure;

public static class ConnectionStringNames
{
    public const string Postgres = "Postgres";

    /// <summary>Login of the read-only role used by text-to-SQL.</summary>
    public const string PostgresReadOnly = "PostgresReadOnly";
    public const string Redis = "Redis";
}
