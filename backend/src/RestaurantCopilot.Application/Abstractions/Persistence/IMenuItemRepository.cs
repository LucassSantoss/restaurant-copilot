using RestaurantCopilot.Domain.MenuItems;

namespace RestaurantCopilot.Application.Abstractions.Persistence;

public interface IMenuItemRepository
{
    Task<IReadOnlyList<MenuItem>> ListAsync(MenuCategory? category, bool availableOnly, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
}
