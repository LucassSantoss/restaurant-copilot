using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RestaurantCopilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetRequiredConnectionString(ConnectionStringNames.Postgres);
        var redisConnectionString = configuration.GetRequiredConnectionString(ConnectionStringNames.Redis);

        services.AddHealthChecks()
            .AddNpgSql(postgresConnectionString, name: "postgres")
            .AddRedis(redisConnectionString, name: "redis");

        return services;
    }

    private static string GetRequiredConnectionString(this IConfiguration configuration, string name) =>
        configuration.GetConnectionString(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException(
                $"Connection string '{name}' is not configured. Set 'ConnectionStrings:{name}' in appsettings or the 'ConnectionStrings__{name}' environment variable.");
}
