using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Domain.Orders;

public sealed class Order : Entity
{
    private readonly List<OrderItem> _items = [];

    private Order(Guid id, int number, DateTimeOffset placedAt, OrderChannel channel, OrderStatus status)
        : base(id)
    {
        Number = number;
        PlacedAt = placedAt;
        Channel = channel;
        Status = status;
    }

    /// <summary>Human-friendly sequential order number shown to the restaurant.</summary>
    public int Number { get; private set; }

    /// <summary>When the order was placed, always stored in UTC.</summary>
    public DateTimeOffset PlacedAt { get; private set; }

    public OrderChannel Channel { get; private set; }

    public OrderStatus Status { get; private set; }

    public IReadOnlyList<OrderItem> Items => _items;

    public Money Total => _items.Aggregate(Money.Zero, (total, item) => total + item.Total);

    public static Result<Order> Place(
        int number,
        DateTimeOffset placedAt,
        OrderChannel channel,
        IReadOnlyCollection<OrderLine> lines)
    {
        if (number <= 0)
        {
            return Error.Validation("Order.Number", "Order number must be positive.");
        }

        if (!Enum.IsDefined(channel))
        {
            return Error.Validation("Order.Channel", "Unknown order channel.");
        }

        if (lines.Count == 0)
        {
            return Error.Validation("Order.Items", "An order must have at least one item.");
        }

        var order = new Order(Guid.CreateVersion7(), number, placedAt.ToUniversalTime(), channel, OrderStatus.Completed);
        foreach (var line in lines)
        {
            var item = OrderItem.Create(line.MenuItemId, line.Quantity, line.UnitPrice);
            if (item.IsFailure)
            {
                return item.Error;
            }

            order._items.Add(item.Value);
        }

        return order;
    }

    public Result Cancel()
    {
        if (Status == OrderStatus.Cancelled)
        {
            return Result.Failure(Error.Conflict("Order.AlreadyCancelled", "Order is already cancelled."));
        }

        Status = OrderStatus.Cancelled;
        return Result.Success();
    }
}
