using RestaurantCopilot.Infrastructure.Persistence.Repositories;

namespace RestaurantCopilot.Infrastructure.Tests.Persistence;

[Collection(PostgresTestCollectionDefinition.Name)]
public sealed class SalesRepositoryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task ListOrderLinesAsync_ReturnsLinesWithDishNamesAndPrices()
    {
        await using var dbContext = fixture.CreateDbContext();
        var repository = new SalesRepository(dbContext);
        var october = (Start: new DateTimeOffset(2026, 10, 1, 3, 0, 0, TimeSpan.Zero), End: new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero));

        var lines = await repository.ListOrderLinesAsync(october.Start, october.End, TestContext.Current.CancellationToken);

        lines.ShouldNotBeEmpty();
        lines.ShouldAllBe(line => line.PlacedAt >= october.Start && line.PlacedAt < october.End);
        lines.ShouldContain(line => line.MenuItemName == "Pizza Margherita" && line.UnitPrice == 64.90m);
    }

    [Fact]
    public async Task GetOrderDateRangeAsync_CoversTheSeedPeriod()
    {
        await using var dbContext = fixture.CreateDbContext();

        var range = await new SalesRepository(dbContext).GetOrderDateRangeAsync(TestContext.Current.CancellationToken);

        range.ShouldNotBeNull();
        var saoPaulo = TimeSpan.FromHours(-3);
        DateOnly.FromDateTime(range.Value.First.ToOffset(saoPaulo).DateTime).ShouldBe(new DateOnly(2026, 8, 3));
        DateOnly.FromDateTime(range.Value.Last.ToOffset(saoPaulo).DateTime).ShouldBe(new DateOnly(2026, 11, 8));
    }
}
