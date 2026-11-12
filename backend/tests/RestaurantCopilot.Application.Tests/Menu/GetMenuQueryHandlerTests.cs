using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Menu.GetMenu;
using RestaurantCopilot.Domain.Common;
using RestaurantCopilot.Domain.MenuItems;

namespace RestaurantCopilot.Application.Tests.Menu;

public sealed class GetMenuQueryHandlerTests
{
    private readonly IMenuItemRepository _repository = Substitute.For<IMenuItemRepository>();
    private readonly GetMenuQueryHandler _handler;

    public GetMenuQueryHandlerTests()
    {
        _handler = new GetMenuQueryHandler(_repository);
    }

    [Fact]
    public async Task HandleAsync_MapsMenuItemsToResponses()
    {
        var pizza = MenuItem.Create("Pizza Margherita", "Clássica.", MenuCategory.Pizza, Money.Create(64.90m).Value).Value;
        _repository.ListAsync(null, false, Arg.Any<CancellationToken>()).Returns([pizza]);

        var result = await _handler.HandleAsync(new GetMenuQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var response = result.Value.ShouldHaveSingleItem();
        response.ShouldBe(new MenuItemResponse(pizza.Id, "Pizza Margherita", "Clássica.", MenuCategory.Pizza, 64.90m, true));
    }

    [Fact]
    public async Task HandleAsync_ForwardsFiltersToRepository()
    {
        _repository.ListAsync(Arg.Any<MenuCategory?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns([]);

        await _handler.HandleAsync(new GetMenuQuery(MenuCategory.Dessert, AvailableOnly: true), TestContext.Current.CancellationToken);

        await _repository.Received(1).ListAsync(MenuCategory.Dessert, true, Arg.Any<CancellationToken>());
    }
}
