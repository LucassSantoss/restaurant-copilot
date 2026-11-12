using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RestaurantCopilot.Infrastructure.Persistence;

/// <summary>
/// Used only by the EF Core CLI (<c>dotnet ef migrations add</c>), so creating migrations
/// does not depend on booting the API. Generating a migration never opens a connection.
/// </summary>
internal sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        PersistenceConfiguration.Configure(options, "Host=localhost;Database=design_time");
        return new ApplicationDbContext(options.Options);
    }
}
