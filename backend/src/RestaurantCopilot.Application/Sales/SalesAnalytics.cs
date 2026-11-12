using System.Globalization;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Common;
using RestaurantCopilot.Domain.Orders;
using RestaurantCopilot.Domain.Restaurants;

namespace RestaurantCopilot.Application.Sales;

/// <summary>
/// Sales aggregation over order lines, in plain C# so the business rules (what counts as revenue,
/// which day an order belongs to) are explicit and unit tested.
/// </summary>
internal sealed class SalesAnalytics(ISalesRepository sales)
{
    private static readonly CultureInfo PortugueseBrazil = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<SalesSummary> SummarizeAsync(
        Restaurant restaurant,
        ReportingPeriod period,
        Guid? menuItemId,
        SalesGrouping grouping,
        CancellationToken cancellationToken)
    {
        var (startUtc, endUtc) = period.ToUtcRange(restaurant);
        var lines = await sales.ListOrderLinesAsync(startUtc, endUtc, cancellationToken);
        return Summarize(period, lines, TimeZoneInfo.FindSystemTimeZoneById(restaurant.TimeZoneId), menuItemId, grouping);
    }

    internal static SalesSummary Summarize(
        ReportingPeriod period,
        IReadOnlyList<OrderLineRow> allLines,
        TimeZoneInfo timeZone,
        Guid? menuItemId,
        SalesGrouping grouping)
    {
        var lines = menuItemId is null ? allLines : allLines.Where(line => line.MenuItemId == menuItemId).ToList();
        var completed = lines.Where(line => line.Status == OrderStatus.Completed).ToList();
        var completedOrders = completed.Select(line => line.OrderId).Distinct().Count();
        var revenue = completed.Sum(line => line.Quantity * line.UnitPrice);

        return new SalesSummary(
            period.From,
            period.To,
            menuItemId is null || lines.Count == 0 ? null : lines[0].MenuItemName,
            completedOrders,
            lines.Where(line => line.Status == OrderStatus.Cancelled).Select(line => line.OrderId).Distinct().Count(),
            revenue,
            completedOrders == 0 ? 0 : decimal.Round(revenue / completedOrders, 2),
            completed.Sum(line => line.Quantity),
            Breakdown(completed, timeZone, grouping));
    }

    private static List<SalesBreakdownRow> Breakdown(List<OrderLineRow> completed, TimeZoneInfo timeZone, SalesGrouping grouping)
    {
        if (grouping == SalesGrouping.None)
        {
            return [];
        }

        var rows = completed
            .GroupBy(line => KeyOf(line, timeZone, grouping))
            .Select(group => (
                Sort: group.Key.Sort,
                Row: new SalesBreakdownRow(
                    group.Key.Label,
                    group.Select(line => line.OrderId).Distinct().Count(),
                    group.Sum(line => line.Quantity * line.UnitPrice),
                    group.Sum(line => line.Quantity))));

        // Dates and weekdays read best in calendar order; dishes and channels by revenue.
        return grouping is SalesGrouping.Day or SalesGrouping.Weekday
            ? rows.OrderBy(row => row.Sort).Select(row => row.Row).ToList()
            : rows.Select(row => row.Row).OrderByDescending(row => row.Revenue).ToList();
    }

    private static (string Label, int Sort) KeyOf(OrderLineRow line, TimeZoneInfo timeZone, SalesGrouping grouping)
    {
        var local = TimeZoneInfo.ConvertTime(line.PlacedAt, timeZone);
        return grouping switch
        {
            SalesGrouping.Day => (local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), DateOnly.FromDateTime(local.DateTime).DayNumber),
            // Monday first, as in the Brazilian business week.
            SalesGrouping.Weekday => (PortugueseBrazil.DateTimeFormat.GetDayName(local.DayOfWeek), ((int)local.DayOfWeek + 6) % 7),
            SalesGrouping.MenuItem => (line.MenuItemName, 0),
            SalesGrouping.Channel => (line.Channel.ToString(), 0),
            _ => throw new ArgumentOutOfRangeException(nameof(grouping), grouping, "Unsupported grouping."),
        };
    }
}
