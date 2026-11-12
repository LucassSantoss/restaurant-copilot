using System.ComponentModel;
using System.Globalization;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Domain.Common;
using RestaurantCopilot.Domain.Reviews;

namespace RestaurantCopilot.Application.Agent.Tools;

internal sealed class ListReviewsTool(IReviewRepository reviews, MenuItemResolver menuItemResolver)
    : AgentTool<ListReviewsTool.Arguments>
{
    internal const int MaxLimit = 50;
    private const int DocumentSourcesLimit = 5;

    public override string Name => "list_reviews";

    public override string Description =>
        "Lists customer reviews (newest first) filtered by period, sentiment, theme, dish and/or maximum rating, "
        + "and returns how many reviews match in total. Use it to count complaints or quote what customers said. "
        + "Themes: delay, taste, packaging, price, service.";

    protected override async Task<Result<AgentToolResult>> ExecuteAsync(
        Arguments arguments,
        AgentContext context,
        CancellationToken cancellationToken)
    {
        if (arguments.Limit is < 1 or > MaxLimit)
        {
            return AgentToolErrors.InvalidArguments($"limit must be between 1 and {MaxLimit}.");
        }

        if ((arguments.PeriodStart is null) != (arguments.PeriodEnd is null))
        {
            return AgentToolErrors.InvalidArguments("Provide both period_start and period_end, or neither.");
        }

        DateTimeOffset? startUtc = null;
        DateTimeOffset? endUtc = null;
        if (arguments is { PeriodStart: { } start, PeriodEnd: { } end })
        {
            var period = Period(start, end);
            if (period.IsFailure)
            {
                return period.Error;
            }

            (startUtc, endUtc) = period.Value.ToUtcRange(context.Restaurant);
        }

        Guid? menuItemId = null;
        if (!string.IsNullOrWhiteSpace(arguments.MenuItem))
        {
            var menuItem = await menuItemResolver.ResolveAsync(arguments.MenuItem, cancellationToken);
            if (menuItem.IsFailure)
            {
                return menuItem.Error;
            }

            menuItemId = menuItem.Value.Id;
        }

        var filter = new ReviewSearchFilter(
            startUtc, endUtc, arguments.Sentiment, arguments.Theme, menuItemId, arguments.MaxRating, arguments.Limit);
        var (rows, total) = await reviews.SearchAsync(filter, cancellationToken);

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(context.Restaurant.TimeZoneId);
        var data = new
        {
            TotalMatching = total,
            Returned = rows.Count,
            Reviews = rows.Select(row => new
            {
                row.OrderNumber,
                Date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(row.CreatedAt, timeZone).DateTime),
                row.Rating,
                Dish = row.MenuItemName,
                row.Sentiment,
                row.Themes,
                row.Comment,
            }),
        };

        IEnumerable<AgentSource> sources =
        [
            AgentSource.Tool(Name, string.Create(CultureInfo.InvariantCulture, $"{total} avaliações encontradas")),
            .. rows.Take(DocumentSourcesLimit).Select(row => AgentSource.Document(row.Id, row.Comment)),
        ];
        return new AgentToolResult(data, sources.ToList());
    }

    internal sealed record Arguments(
        [property: Description("Optional first day, inclusive (yyyy-MM-dd). Use together with period_end.")] DateOnly? PeriodStart = null,
        [property: Description("Optional last day, inclusive (yyyy-MM-dd).")] DateOnly? PeriodEnd = null,
        [property: Description("Optional sentiment filter.")] ReviewSentiment? Sentiment = null,
        [property: Description("Optional theme filter.")] ReviewTheme? Theme = null,
        [property: Description("Optional dish name.")] string? MenuItem = null,
        [property: Description("Optional maximum star rating (1-5), e.g. 2 for bad reviews.")] int? MaxRating = null,
        [property: Description("How many reviews to return (1-50).")] int Limit = 20);
}
