using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Menu.GetMenu;

internal sealed class GetMenuQueryHandler(IMenuItemRepository menuItems)
    : IQueryHandler<GetMenuQuery, IReadOnlyList<MenuItemResponse>>
{
    public async Task<Result<IReadOnlyList<MenuItemResponse>>> HandleAsync(
        GetMenuQuery query,
        CancellationToken cancellationToken)
    {
        var items = await menuItems.ListAsync(query.Category, query.AvailableOnly, cancellationToken);

        return items.Select(MenuItemResponse.FromEntity).ToList();
    }
}
