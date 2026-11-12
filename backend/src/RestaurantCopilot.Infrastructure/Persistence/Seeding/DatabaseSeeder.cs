using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantCopilot.Domain.Common;
using RestaurantCopilot.Domain.MenuItems;
using RestaurantCopilot.Domain.Orders;
using RestaurantCopilot.Domain.Restaurants;
using RestaurantCopilot.Domain.Reviews;

namespace RestaurantCopilot.Infrastructure.Persistence.Seeding;

/// <summary>
/// Loads the fictitious seed data into an empty database. Every record goes through the
/// domain factories, so seed data obeys the same invariants as data created through the API.
/// </summary>
internal sealed class DatabaseSeeder(ApplicationDbContext dbContext, ILogger<DatabaseSeeder> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task SeedAsync(string seedDirectory, CancellationToken cancellationToken)
    {
        if (await dbContext.Restaurants.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Database already seeded, skipping");
            return;
        }

        var restaurantSeed = await ReadAsync<RestaurantSeed>(seedDirectory, "restaurant.json", cancellationToken);
        var menuSeed = await ReadAsync<List<MenuItemSeed>>(seedDirectory, "menu.json", cancellationToken);
        var orderSeed = await ReadAsync<List<OrderSeed>>(seedDirectory, "orders.json", cancellationToken);
        var reviewSeed = await ReadAsync<List<ReviewSeed>>(seedDirectory, "reviews.json", cancellationToken);

        var restaurant = Unwrap(Restaurant.Create(
            restaurantSeed.Name, restaurantSeed.Cuisine, restaurantSeed.City, restaurantSeed.TimeZoneId));
        var menuByKey = menuSeed.ToDictionary(seed => seed.Key, CreateMenuItem);
        var ordersByNumber = orderSeed.ToDictionary(seed => seed.Number, seed => CreateOrder(seed, menuByKey));
        var reviews = reviewSeed.Select(seed => CreateReview(seed, ordersByNumber, menuByKey));

        dbContext.Restaurants.Add(restaurant);
        dbContext.MenuItems.AddRange(menuByKey.Values);
        dbContext.Orders.AddRange(ordersByNumber.Values);
        dbContext.Reviews.AddRange(reviews);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seeded {MenuItemCount} menu items, {OrderCount} orders and {ReviewCount} reviews",
            menuByKey.Count,
            ordersByNumber.Count,
            reviewSeed.Count);
    }

    private static MenuItem CreateMenuItem(MenuItemSeed seed) =>
        Unwrap(MenuItem.Create(
            seed.Name,
            seed.Description,
            Enum.Parse<MenuCategory>(seed.Category),
            Unwrap(Money.Create(seed.Price))));

    private static Order CreateOrder(OrderSeed seed, Dictionary<string, MenuItem> menuByKey)
    {
        var lines = seed.Items
            .Select(item => menuByKey[item.MenuItem])
            .Zip(seed.Items, (menuItem, item) => new OrderLine(menuItem.Id, item.Quantity, menuItem.Price))
            .ToList();

        var order = Unwrap(Order.Place(seed.Number, seed.PlacedAt, Enum.Parse<OrderChannel>(seed.Channel), lines));
        if (Enum.Parse<OrderStatus>(seed.Status) == OrderStatus.Cancelled)
        {
            Unwrap(order.Cancel());
        }

        return order;
    }

    private static Review CreateReview(
        ReviewSeed seed,
        Dictionary<int, Order> ordersByNumber,
        Dictionary<string, MenuItem> menuByKey) =>
        Unwrap(Review.Create(
            ordersByNumber[seed.OrderNumber].Id,
            seed.MenuItem is null ? null : menuByKey[seed.MenuItem].Id,
            Unwrap(Rating.Create(seed.Rating)),
            seed.Comment,
            seed.CreatedAt));

    private static async Task<T> ReadAsync<T>(string directory, string fileName, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(Path.Combine(directory, fileName));
        return await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions, cancellationToken)
            ?? throw new InvalidDataException($"Seed file '{fileName}' is empty.");
    }

    // Invalid seed data is a bug in the committed files, not a business error: fail loudly.
    private static T Unwrap<T>(Result<T> result) =>
        result.IsSuccess ? result.Value : throw new InvalidDataException($"Invalid seed data: {result.Error.Message}");

    private static void Unwrap(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidDataException($"Invalid seed data: {result.Error.Message}");
        }
    }
}
