using RestaurantCopilot.Domain.MenuItems;

namespace RestaurantCopilot.Application.Menu.GetMenu;

public sealed record MenuItemResponse(
    Guid Id,
    string Name,
    string Description,
    MenuCategory Category,
    decimal Price,
    bool IsAvailable)
{
    public static MenuItemResponse FromEntity(MenuItem item) =>
        new(item.Id, item.Name, item.Description, item.Category, item.Price.Amount, item.IsAvailable);
}
