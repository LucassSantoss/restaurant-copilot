using RestaurantCopilot.Domain.Common;
using RestaurantCopilot.Domain.Orders;
using RestaurantCopilot.Domain.Restaurants;
using RestaurantCopilot.Domain.Reviews;

namespace RestaurantCopilot.Application.Tests.TestDoubles;

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 11, 11, 15, 0, 0, TimeSpan.Zero);

    public static Restaurant Restaurant() =>
        Domain.Restaurants.Restaurant.Create("Bella Massa", "Italiana", "São Paulo", "America/Sao_Paulo").Value;

    public static Order Order(int number = 1001) =>
        Domain.Orders.Order.Place(number, Now.AddDays(-1), OrderChannel.Delivery, [new(Guid.NewGuid(), 1, Money.Create(50m).Value)]).Value;

    public static Review Review(string comment = "Chegou atrasado.", int rating = 2) =>
        Domain.Reviews.Review.Create(Guid.NewGuid(), null, Rating.Create(rating).Value, comment, Now).Value;
}
