using RestaurantCopilot.Domain.Restaurants;

namespace RestaurantCopilot.Domain.Tests.Restaurants;

public sealed class RestaurantTests
{
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var restaurant = Restaurant.Create("Bella Massa Cantina", "Italiana", "São Paulo", "America/Sao_Paulo").Value;

        restaurant.TimeZoneId.ShouldBe("America/Sao_Paulo");
    }

    [Fact]
    public void Create_WithUnknownTimeZone_Fails()
    {
        var result = Restaurant.Create("Bella Massa Cantina", "Italiana", "São Paulo", "Mars/Olympus_Mons");

        result.Error.Code.ShouldBe("Restaurant.TimeZone");
    }
}
