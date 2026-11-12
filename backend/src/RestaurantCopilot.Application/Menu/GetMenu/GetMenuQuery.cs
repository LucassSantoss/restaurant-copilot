using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Domain.MenuItems;

namespace RestaurantCopilot.Application.Menu.GetMenu;

public sealed record GetMenuQuery(MenuCategory? Category = null, bool AvailableOnly = false)
    : IQuery<IReadOnlyList<MenuItemResponse>>;
