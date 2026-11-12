using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RestaurantCopilot.Infrastructure.Persistence.Seeding;

namespace RestaurantCopilot.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>Applies migrations and loads seed data according to <see cref="DatabaseOptions"/>.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        if (options.ApplyMigrationsOnStartup)
        {
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(options.SeedPath))
        {
            var contentRoot = scope.ServiceProvider.GetRequiredService<IHostEnvironment>().ContentRootPath;
            var seedDirectory = Path.GetFullPath(options.SeedPath, contentRoot);
            await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync(seedDirectory, cancellationToken);
        }
    }
}
