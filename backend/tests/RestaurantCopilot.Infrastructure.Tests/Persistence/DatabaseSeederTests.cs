using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RestaurantCopilot.Domain.Orders;
using RestaurantCopilot.Infrastructure.Persistence.Seeding;

namespace RestaurantCopilot.Infrastructure.Tests.Persistence;

[Collection(PostgresTestCollectionDefinition.Name)]
public sealed class DatabaseSeederTests(PostgresFixture fixture)
{
    [Fact]
    public async Task SeedAsync_LoadsEverySeedFile()
    {
        await using var dbContext = fixture.CreateDbContext();
        var cancellationToken = TestContext.Current.CancellationToken;

        (await dbContext.Restaurants.CountAsync(cancellationToken)).ShouldBe(1);
        (await dbContext.MenuItems.CountAsync(cancellationToken)).ShouldBe(24);
        (await dbContext.Orders.CountAsync(cancellationToken)).ShouldBe(1489);
        (await dbContext.Reviews.CountAsync(cancellationToken)).ShouldBe(242);
        (await dbContext.Orders.CountAsync(order => order.Status == OrderStatus.Cancelled, cancellationToken)).ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task SeedAsync_WhenAlreadySeeded_DoesNothing()
    {
        await using var dbContext = fixture.CreateDbContext();
        var seeder = new DatabaseSeeder(dbContext, NullLogger<DatabaseSeeder>.Instance);

        await seeder.SeedAsync(PostgresFixture.SeedDirectory, TestContext.Current.CancellationToken);

        (await dbContext.MenuItems.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(24);
    }

    [Fact]
    public async Task SeedAsync_PreservesOrderItemsAndPrices()
    {
        await using var dbContext = fixture.CreateDbContext();

        var order = await dbContext.Orders
            .Include(o => o.Items)
            .SingleAsync(o => o.Number == 1001, TestContext.Current.CancellationToken);

        order.Items.ShouldNotBeEmpty();
        order.Total.Amount.ShouldBeGreaterThan(0m);
        order.PlacedAt.Offset.ShouldBe(TimeSpan.Zero);
    }
}
