using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Domain.Orders;

/// <summary>Input for a line when placing an order.</summary>
public sealed record OrderLine(Guid MenuItemId, int Quantity, Money UnitPrice);
