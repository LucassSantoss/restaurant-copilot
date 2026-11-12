using RestaurantCopilot.Domain.MenuItems;
using RestaurantCopilot.Infrastructure.Persistence.Repositories;

namespace RestaurantCopilot.Infrastructure.Tests.Persistence;

[Collection(PostgresTestCollectionDefinition.Name)]
public sealed class MenuItemRepositoryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task ListAsync_WithoutFilters_ReturnsWholeMenuInCategoryOrder()
    {
        await using var dbContext = fixture.CreateDbContext();
        var repository = new MenuItemRepository(dbContext);

        var items = await repository.ListAsync(null, availableOnly: false, TestContext.Current.CancellationToken);

        items.Count.ShouldBe(24);
        items.Select(item => item.Category).ShouldBeInOrder();
    }

    [Fact]
    public async Task ListAsync_WithCategory_ReturnsOnlyThatCategory()
    {
        await using var dbContext = fixture.CreateDbContext();
        var repository = new MenuItemRepository(dbContext);

        var desserts = await repository.ListAsync(MenuCategory.Dessert, availableOnly: false, TestContext.Current.CancellationToken);

        desserts.Count.ShouldBe(3);
        desserts.ShouldAllBe(item => item.Category == MenuCategory.Dessert);
    }

    [Fact]
    public async Task ListAsync_ReadsMoneyValueObject()
    {
        await using var dbContext = fixture.CreateDbContext();
        var repository = new MenuItemRepository(dbContext);

        var drinks = await repository.ListAsync(MenuCategory.Drink, availableOnly: true, TestContext.Current.CancellationToken);

        drinks.Single(item => item.Name == "Água Mineral").Price.Amount.ShouldBe(5.90m);
    }
}
