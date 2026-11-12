using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Common;

public static class ApplicationErrors
{
    public static readonly Error RestaurantNotConfigured = Error.Failure(
        "Restaurant.NotConfigured", "The restaurant profile has not been loaded yet.");

    public static Error OrderNotFound(int number) => Error.NotFound("Order.NotFound", $"Order {number} does not exist.");

    public static Error MenuItemNotFound(Guid id) => Error.NotFound("MenuItem.NotFound", $"Menu item {id} does not exist.");
}
