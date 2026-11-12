using Microsoft.EntityFrameworkCore;
using RestaurantCopilot.Application.Abstractions.Persistence;

namespace RestaurantCopilot.Infrastructure.Persistence.Repositories;

internal sealed class SalesRepository(ApplicationDbContext dbContext) : ISalesRepository
{
    public async Task<IReadOnlyList<OrderLineRow>> ListOrderLinesAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken)
    {
        var lines = await dbContext.Orders
            .AsNoTracking()
            .Where(order => order.PlacedAt >= startUtc && order.PlacedAt < endUtc)
            .SelectMany(order => order.Items, (order, item) => new { order, item })
            .Join(
                dbContext.MenuItems,
                line => line.item.MenuItemId,
                menuItem => menuItem.Id,
                (line, menuItem) => new
                {
                    OrderId = line.order.Id,
                    line.order.PlacedAt,
                    line.order.Channel,
                    line.order.Status,
                    line.item.MenuItemId,
                    MenuItemName = menuItem.Name,
                    line.item.Quantity,
                    line.item.UnitPrice,
                })
            .ToListAsync(cancellationToken);

        return lines
            .Select(line => new OrderLineRow(
                line.OrderId, line.PlacedAt, line.Channel, line.Status, line.MenuItemId, line.MenuItemName, line.Quantity, line.UnitPrice.Amount))
            .ToList();
    }

    public async Task<(DateTimeOffset First, DateTimeOffset Last)?> GetOrderDateRangeAsync(CancellationToken cancellationToken)
    {
        var range = await dbContext.Orders
            .GroupBy(_ => 1)
            .Select(group => new { First = group.Min(order => order.PlacedAt), Last = group.Max(order => order.PlacedAt) })
            .FirstOrDefaultAsync(cancellationToken);

        return range is null ? null : (range.First, range.Last);
    }
}
