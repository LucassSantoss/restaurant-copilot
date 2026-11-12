using System.Net;
using System.Net.Http.Json;
using RestaurantCopilot.Api.Tests.Infrastructure;

namespace RestaurantCopilot.Api.Tests.Menu;

[Collection(ApiTestCollectionDefinition.Name)]
public sealed class MenuEndpointTests(ApiFactory factory)
{
    [Fact]
    public async Task Get_ReturnsSeededMenu()
    {
        using var client = factory.CreateClient();

        var items = await client.GetFromJsonAsync<List<MenuItemBody>>("/api/menu", TestContext.Current.CancellationToken);

        items.ShouldNotBeNull();
        items.Count.ShouldBe(24);
        items.ShouldContain(item => item.Name == "Pizza Margherita" && item.Price == 64.90m && item.Category == "Pizza");
    }

    [Fact]
    public async Task Get_WithCategory_FiltersItems()
    {
        using var client = factory.CreateClient();

        var items = await client.GetFromJsonAsync<List<MenuItemBody>>("/api/menu?category=Dessert", TestContext.Current.CancellationToken);

        items.ShouldNotBeNull();
        items.ShouldAllBe(item => item.Category == "Dessert");
        items.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Get_WithUnknownCategory_ReturnsBadRequest()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/menu?category=Sushi", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private sealed record MenuItemBody(Guid Id, string Name, string Category, decimal Price, bool IsAvailable);
}
