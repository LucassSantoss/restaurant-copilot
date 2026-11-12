namespace RestaurantCopilot.Application.Sales;

/// <param name="Revenue">Revenue of completed orders, in BRL. Cancelled orders never count as revenue.</param>
/// <param name="AverageTicket">Revenue per completed order, in BRL.</param>
public sealed record SalesSummary(
    DateOnly From,
    DateOnly To,
    string? MenuItem,
    int CompletedOrders,
    int CancelledOrders,
    decimal Revenue,
    decimal AverageTicket,
    int ItemsSold,
    IReadOnlyList<SalesBreakdownRow> Breakdown);

/// <param name="Key">Group label: a date (yyyy-MM-dd), a weekday name, a dish or a channel.</param>
public sealed record SalesBreakdownRow(string Key, int Orders, decimal Revenue, int ItemsSold);
