using System.Text.Json;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Agent;
using RestaurantCopilot.Application.Agent.Tools;
using RestaurantCopilot.Application.Sales;
using RestaurantCopilot.Application.Tests.TestDoubles;
using RestaurantCopilot.Domain.Common;
using RestaurantCopilot.Domain.MenuItems;

namespace RestaurantCopilot.Application.Tests.Agent;

public sealed class AgentToolTests
{
    private static readonly AgentContext Context = new(TestData.Restaurant(), new DateOnly(2026, 11, 11));

    private readonly ISalesRepository _sales = Substitute.For<ISalesRepository>();
    private readonly IMenuItemRepository _menuItems = Substitute.For<IMenuItemRepository>();
    private readonly SearchSalesTool _tool;

    public AgentToolTests()
    {
        _menuItems.ListAsync(null, false, Arg.Any<CancellationToken>()).Returns(
        [
            MenuItem.Create("Lasanha à Bolonhesa", "", MenuCategory.Pasta, Money.Create(54.90m).Value).Value,
            MenuItem.Create("Pizza Margherita", "", MenuCategory.Pizza, Money.Create(64.90m).Value).Value,
            MenuItem.Create("Pizza Calabresa", "", MenuCategory.Pizza, Money.Create(62.90m).Value).Value,
        ]);
        _sales.ListOrderLinesAsync(default, default, TestContext.Current.CancellationToken).ReturnsForAnyArgs([]);
        _tool = new SearchSalesTool(new SalesAnalytics(_sales), new MenuItemResolver(_menuItems));
    }

    [Fact]
    public void ParametersSchema_UsesSnakeCaseWithDescriptionsAndRequiredPeriod()
    {
        var schema = _tool.ParametersSchema;

        schema.GetProperty("type").GetString().ShouldBe("object");
        var properties = schema.GetProperty("properties");
        properties.GetProperty("period_start").GetProperty("description").GetString().ShouldNotBeNullOrWhiteSpace();
        properties.GetProperty("menu_item").GetProperty("type").GetString().ShouldBe("string");
        properties.GetProperty("group_by").GetProperty("enum").EnumerateArray().Select(value => value.GetString())
            .ShouldBe(["none", "day", "weekday", "menu_item", "channel"]);
        schema.GetProperty("required").EnumerateArray().Select(value => value.GetString())
            .ShouldBe(["period_start", "period_end"], ignoreOrder: true);
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingRequiredArgument_ReturnsInvalidArguments()
    {
        var result = await _tool.ExecuteAsync(Json("""{ "period_end": "2026-10-31" }"""), Context, TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("AgentTool.InvalidArguments");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvertedPeriod_ReturnsInvalidArguments()
    {
        var result = await _tool.ExecuteAsync(
            Json("""{ "period_start": "2026-10-31", "period_end": "2026-10-01" }"""), Context, TestContext.Current.CancellationToken);

        result.Error.Message.ShouldContain("period_end");
    }

    [Fact]
    public async Task ExecuteAsync_ResolvesDishIgnoringCaseAndAccents()
    {
        var result = await _tool.ExecuteAsync(
            Json("""{ "period_start": "2026-10-01", "period_end": "2026-10-31", "menu_item": "lasanha a bolonhesa" }"""),
            Context,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Sources.ShouldHaveSingleItem().Description.ShouldBe("Vendas de 01/10/2026 a 31/10/2026 (Lasanha à Bolonhesa)");
    }

    [Fact]
    public async Task ExecuteAsync_WithAmbiguousDish_ListsTheCandidates()
    {
        var result = await _tool.ExecuteAsync(
            Json("""{ "period_start": "2026-10-01", "period_end": "2026-10-31", "menu_item": "pizza" }"""),
            Context,
            TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe("AgentTool.UnknownMenuItem");
        result.Error.Message.ShouldContain("Pizza Margherita; Pizza Calabresa");
    }

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;
}
