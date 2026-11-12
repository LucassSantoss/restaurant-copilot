using RestaurantCopilot.Domain.MenuItems;

namespace RestaurantCopilot.Application.Abstractions.Persistence;

public interface IMenuItemRepository
{
    Task<IReadOnlyList<MenuItem>> ListAsync(MenuCategory? category, bool availableOnly, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Menu items that have no search document yet, for incremental indexing.</summary>
    Task<IReadOnlyList<MenuItem>> ListNotIndexedAsync(CancellationToken cancellationToken);
}
