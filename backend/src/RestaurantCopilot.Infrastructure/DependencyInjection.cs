using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Abstractions.Search;
using RestaurantCopilot.Infrastructure.Ai.Gemini;
using RestaurantCopilot.Infrastructure.Persistence;
using RestaurantCopilot.Infrastructure.Persistence.Repositories;
using RestaurantCopilot.Infrastructure.Persistence.Seeding;
using RestaurantCopilot.Infrastructure.Search;

namespace RestaurantCopilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetRequiredConnectionString(ConnectionStringNames.Postgres);
        var redisConnectionString = configuration.GetRequiredConnectionString(ConnectionStringNames.Redis);

        services.AddPersistence(configuration, postgresConnectionString);
        services.AddGemini(configuration);

        services.AddHealthChecks()
            .AddNpgSql(postgresConnectionString, name: "postgres")
            .AddRedis(redisConnectionString, name: "redis")
            .AddGeminiHealthCheck();

        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration, string connectionString)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.AddDbContext<ApplicationDbContext>(options => PersistenceConfiguration.Configure(options, connectionString));
        services.AddScoped<DatabaseSeeder>();

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IRestaurantRepository, RestaurantRepository>();
        services.AddScoped<IMenuItemRepository, MenuItemRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<ISalesRepository, SalesRepository>();
        services.AddScoped<IVectorStore, PgvectorVectorStore>();
    }

    private static string GetRequiredConnectionString(this IConfiguration configuration, string name) =>
        configuration.GetConnectionString(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException(
                $"Connection string '{name}' is not configured. Set 'ConnectionStrings:{name}' in appsettings or the 'ConnectionStrings__{name}' environment variable.");
}
