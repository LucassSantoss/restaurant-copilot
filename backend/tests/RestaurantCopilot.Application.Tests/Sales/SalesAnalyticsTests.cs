using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Common;
using RestaurantCopilot.Application.Sales;
using RestaurantCopilot.Domain.Orders;

namespace RestaurantCopilot.Application.Tests.Sales;

public sealed class SalesAnalyticsTests
{
    private static readonly TimeZoneInfo SaoPaulo = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    private static readonly ReportingPeriod Week = new(new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 11));
    private static readonly Guid Pizza = Guid.NewGuid();
    private static readonly Guid Soda = Guid.NewGuid();

    // Order A: Monday lunch, pizza x2 + soda. Order B: Tuesday 01:00 UTC = Monday 22:00 local, pizza.
    // Order C: cancelled pickup on Friday.
    private static readonly Guid OrderA = Guid.NewGuid();
    private static readonly Guid OrderB = Guid.NewGuid();
    private static readonly Guid OrderC = Guid.NewGuid();

    private static readonly OrderLineRow[] Lines =
    [
        new(OrderA, new DateTimeOffset(2026, 10, 5, 15, 0, 0, TimeSpan.Zero), OrderChannel.Delivery, OrderStatus.Completed, Pizza, "Pizza", 2, 60m),
        new(OrderA, new DateTimeOffset(2026, 10, 5, 15, 0, 0, TimeSpan.Zero), OrderChannel.Delivery, OrderStatus.Completed, Soda, "Refri", 1, 8m),
        new(OrderB, new DateTimeOffset(2026, 10, 6, 1, 0, 0, TimeSpan.Zero), OrderChannel.DineIn, OrderStatus.Completed, Pizza, "Pizza", 1, 60m),
        new(OrderC, new DateTimeOffset(2026, 10, 9, 23, 0, 0, TimeSpan.Zero), OrderChannel.Pickup, OrderStatus.Cancelled, Pizza, "Pizza", 3, 60m),
    ];

    [Fact]
    public void Summarize_CountsOnlyCompletedOrdersAsRevenue()
    {
        var summary = SalesAnalytics.Summarize(Week, Lines, SaoPaulo, menuItemId: null, SalesGrouping.None);

        summary.CompletedOrders.ShouldBe(2);
        summary.CancelledOrders.ShouldBe(1);
        summary.Revenue.ShouldBe(188m);
        summary.AverageTicket.ShouldBe(94m);
        summary.ItemsSold.ShouldBe(4);
        summary.Breakdown.ShouldBeEmpty();
    }

    [Fact]
    public void Summarize_ForOneDish_OnlyCountsThatDish()
    {
        var summary = SalesAnalytics.Summarize(Week, Lines, SaoPaulo, Soda, SalesGrouping.None);

        summary.MenuItem.ShouldBe("Refri");
        summary.Revenue.ShouldBe(8m);
        summary.CompletedOrders.ShouldBe(1);
    }

    [Fact]
    public void Summarize_ByWeekday_UsesRestaurantLocalTime()
    {
        var summary = SalesAnalytics.Summarize(Week, Lines, SaoPaulo, null, SalesGrouping.Weekday);

        // Order B happened on Tuesday in UTC but on Monday night in São Paulo.
        summary.Breakdown.ShouldHaveSingleItem().ShouldBe(new SalesBreakdownRow("segunda-feira", 2, 188m, 4));
    }

    [Fact]
    public void Summarize_ByMenuItem_OrdersByRevenue()
    {
        var summary = SalesAnalytics.Summarize(Week, Lines, SaoPaulo, null, SalesGrouping.MenuItem);

        summary.Breakdown.Select(row => row.Key).ShouldBe(["Pizza", "Refri"]);
        summary.Breakdown[0].Revenue.ShouldBe(180m);
    }

    [Fact]
    public void Summarize_ByDay_IsInCalendarOrder()
    {
        var summary = SalesAnalytics.Summarize(Week, Lines, SaoPaulo, null, SalesGrouping.Day);

        summary.Breakdown.ShouldHaveSingleItem().Key.ShouldBe("2026-10-05");
    }
}
