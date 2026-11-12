using RestaurantCopilot.Domain.Orders;

namespace RestaurantCopilot.Application.Abstractions.Persistence;

public interface ISalesRepository
{
    /// <summary>Every order line of the orders placed in [startUtc, endUtc), cancelled orders included.</summary>
    Task<IReadOnlyList<OrderLineRow>> ListOrderLinesAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken);

    /// <summary>When the first and last orders were placed; null when there are no orders.</summary>
    Task<(DateTimeOffset First, DateTimeOffset Last)?> GetOrderDateRangeAsync(CancellationToken cancellationToken);
}

public sealed record OrderLineRow(
    Guid OrderId,
    DateTimeOffset PlacedAt,
    OrderChannel Channel,
    OrderStatus Status,
    Guid MenuItemId,
    string MenuItemName,
    int Quantity,
    decimal UnitPrice);
