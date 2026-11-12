using Microsoft.EntityFrameworkCore;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Domain.MenuItems;

namespace RestaurantCopilot.Infrastructure.Persistence.Repositories;

internal sealed class MenuItemRepository(ApplicationDbContext dbContext) : IMenuItemRepository
{
    public async Task<IReadOnlyList<MenuItem>> ListAsync(
        MenuCategory? category,
        bool availableOnly,
        CancellationToken cancellationToken)
    {
        var query = dbContext.MenuItems.AsNoTracking();

        if (category is not null)
        {
            query = query.Where(item => item.Category == category);
        }

        if (availableOnly)
        {
            query = query.Where(item => item.IsAvailable);
        }

        var items = await query.ToListAsync(cancellationToken);

        // Sorted in memory: the menu is small, and categories must follow the enum (menu) order,
        // not the alphabetical order of their stored text.
        return [.. items.OrderBy(item => item.Category).ThenBy(item => item.Name, StringComparer.Ordinal)];
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.MenuItems.AnyAsync(item => item.Id == id, cancellationToken);
}
