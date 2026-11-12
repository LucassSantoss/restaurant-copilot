using RestaurantCopilot.Api.Http;
using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Menu.GetMenu;
using RestaurantCopilot.Domain.MenuItems;

namespace RestaurantCopilot.Api.Endpoints;

internal static class MenuEndpoints
{
    public static IEndpointRouteBuilder MapMenuEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/menu", GetMenuAsync)
            .WithTags("Menu")
            .WithSummary("Lists the menu, optionally filtered by category and availability.")
            .Produces<IReadOnlyList<MenuItemResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> GetMenuAsync(
        IQueryHandler<GetMenuQuery, IReadOnlyList<MenuItemResponse>> handler,
        CancellationToken cancellationToken,
        MenuCategory? category = null,
        bool availableOnly = false)
    {
        var result = await handler.HandleAsync(new GetMenuQuery(category, availableOnly), cancellationToken);
        return result.ToHttpResult();
    }
}
