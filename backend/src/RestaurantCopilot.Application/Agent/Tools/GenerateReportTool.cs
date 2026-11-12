using System.ComponentModel;
using System.Globalization;
using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Common;
using RestaurantCopilot.Application.Reviews.GetReviewInsights;
using RestaurantCopilot.Application.Sales;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Agent.Tools;

/// <summary>Ready-made bundles of figures for broad questions, saving the model several tool calls.</summary>
internal sealed class GenerateReportTool(
    SalesAnalytics salesAnalytics,
    IQueryHandler<GetReviewInsightsQuery, ReviewInsightsResponse> reviewInsights)
    : AgentTool<GenerateReportTool.Arguments>
{
    public override string Name => "generate_report";

    public override string Description =>
        "Builds a report for a period. sales_overview: totals, change versus the previous period of the same length, "
        + "and breakdowns by weekday and channel. reviews_overview: sentiment, themes and most complained dishes. "
        + "menu_performance: revenue per dish plus the dishes with most negative reviews.";

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

        Result<object> report = arguments.Type switch
        {
            ReportType.SalesOverview => await SalesOverviewAsync(context, period.Value, cancellationToken),
            ReportType.ReviewsOverview => await ReviewsOverviewAsync(period.Value, cancellationToken),
            ReportType.MenuPerformance => await MenuPerformanceAsync(context, period.Value, cancellationToken),
            _ => AgentToolErrors.InvalidArguments("Unknown report type."),
        };
        if (report.IsFailure)
        {
            return report.Error;
        }

        var description = string.Create(
            CultureInfo.InvariantCulture,
            $"Relatório {arguments.Type} de {period.Value.From:dd/MM/yyyy} a {period.Value.To:dd/MM/yyyy}");
        return new AgentToolResult(report.Value, [AgentSource.Tool(Name, description)]);
    }

    private async Task<Result<object>> SalesOverviewAsync(AgentContext context, ReportingPeriod period, CancellationToken cancellationToken)
    {
        var previous = new ReportingPeriod(period.From.AddDays(-period.Days), period.From.AddDays(-1));
        var current = await salesAnalytics.SummarizeAsync(context.Restaurant, period, null, SalesGrouping.Weekday, cancellationToken);
        var byChannel = await salesAnalytics.SummarizeAsync(context.Restaurant, period, null, SalesGrouping.Channel, cancellationToken);
        var before = await salesAnalytics.SummarizeAsync(context.Restaurant, previous, null, SalesGrouping.None, cancellationToken);

        return new
        {
            Current = current,
            ByChannel = byChannel.Breakdown,
            PreviousPeriod = before,
            RevenueChangePercent = before.Revenue == 0 ? (decimal?)null : decimal.Round((current.Revenue - before.Revenue) / before.Revenue * 100, 1),
        };
    }

    private async Task<Result<object>> ReviewsOverviewAsync(ReportingPeriod period, CancellationToken cancellationToken)
    {
        var insights = await reviewInsights.HandleAsync(new GetReviewInsightsQuery(period.From, period.To), cancellationToken);
        return insights.IsSuccess ? insights.Value : insights.Error;
    }

    private async Task<Result<object>> MenuPerformanceAsync(AgentContext context, ReportingPeriod period, CancellationToken cancellationToken)
    {
        var sales = await salesAnalytics.SummarizeAsync(context.Restaurant, period, null, SalesGrouping.MenuItem, cancellationToken);
        var insights = await reviewInsights.HandleAsync(new GetReviewInsightsQuery(period.From, period.To), cancellationToken);
        if (insights.IsFailure)
        {
            return insights.Error;
        }

        return new { RevenueByDish = sales.Breakdown, insights.Value.MostComplainedDishes };
    }

    internal enum ReportType
    {
        SalesOverview,
        ReviewsOverview,
        MenuPerformance,
    }

    internal sealed record Arguments(
        [property: Description("Which report to build.")] ReportType Type,
        [property: Description("First day of the period, inclusive (yyyy-MM-dd).")] DateOnly PeriodStart,
        [property: Description("Last day of the period, inclusive (yyyy-MM-dd).")] DateOnly PeriodEnd);
}
