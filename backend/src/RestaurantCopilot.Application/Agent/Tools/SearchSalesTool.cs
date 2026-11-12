using System.ComponentModel;
using System.Globalization;
using RestaurantCopilot.Application.Sales;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Agent.Tools;

internal sealed class SearchSalesTool(SalesAnalytics salesAnalytics, MenuItemResolver menuItemResolver)
    : AgentTool<SearchSalesTool.Arguments>
{
    public override string Name => "search_sales";

    public override string Description =>
        "Sales figures for a period: revenue in BRL (completed orders only), completed and cancelled orders, "
        + "average ticket and items sold. Optionally restricted to one dish and broken down by day, weekday, "
        + "dish (menu_item) or channel (delivery, pickup, dine_in). To compare periods, call it once per period.";

    protected override async Task<Result<AgentToolResult>> ExecuteAsync(
        Arguments arguments,
        AgentContext context,
        CancellationToken cancellationToken)
    {
        var period = Period(arguments.PeriodStart, arguments.PeriodEnd);
        if (period.IsFailure)
        {
            return period.Error;
        }

        Guid? menuItemId = null;
        var dishLabel = string.Empty;
        if (!string.IsNullOrWhiteSpace(arguments.MenuItem))
        {
            var menuItem = await menuItemResolver.ResolveAsync(arguments.MenuItem, cancellationToken);
            if (menuItem.IsFailure)
            {
                return menuItem.Error;
            }

            menuItemId = menuItem.Value.Id;
            dishLabel = $" ({menuItem.Value.Name})";
        }

        var summary = await salesAnalytics.SummarizeAsync(
            context.Restaurant, period.Value, menuItemId, arguments.GroupBy, cancellationToken);

        var description = string.Create(
            CultureInfo.InvariantCulture,
            $"Vendas de {summary.From:dd/MM/yyyy} a {summary.To:dd/MM/yyyy}{dishLabel}");
        return new AgentToolResult(summary, [AgentSource.Tool(Name, description)]);
    }

    internal sealed record Arguments(
        [property: Description("First day of the period, inclusive (yyyy-MM-dd).")] DateOnly PeriodStart,
        [property: Description("Last day of the period, inclusive (yyyy-MM-dd).")] DateOnly PeriodEnd,
        [property: Description("Optional dish name, to get the sales of that dish only.")] string? MenuItem = null,
        [property: Description("Optional breakdown of the totals.")] SalesGrouping GroupBy = SalesGrouping.None);
}
