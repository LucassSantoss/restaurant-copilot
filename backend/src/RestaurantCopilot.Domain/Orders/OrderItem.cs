using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Domain.Orders;

/// <summary>
/// A line of an order. The unit price is copied from the menu when the order is placed,
/// so later price changes do not rewrite sales history.
/// </summary>
public sealed class OrderItem : Entity
{
    private OrderItem(Guid id, Guid menuItemId, int quantity, Money unitPrice)
        : base(id)
    {
        MenuItemId = menuItemId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid MenuItemId { get; private set; }

    public int Quantity { get; private set; }

    public Money UnitPrice { get; private set; }

    public Money Total => UnitPrice * Quantity;

    internal static Result<OrderItem> Create(Guid menuItemId, int quantity, Money unitPrice)
    {
        if (menuItemId == Guid.Empty)
        {
            return Error.Validation("OrderItem.MenuItem", "Order item must reference a menu item.");
        }

        if (quantity <= 0)
        {
            return Error.Validation("OrderItem.Quantity", "Quantity must be greater than zero.");
        }

        return new OrderItem(Guid.CreateVersion7(), menuItemId, quantity, unitPrice);
    }
}
