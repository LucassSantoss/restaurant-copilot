using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Domain.Tests.Common;

public sealed class MoneyTests
{
    [Fact]
    public void Create_WithNegativeAmount_Fails()
    {
        var result = Money.Create(-0.01m);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Money.Negative");
    }

    [Fact]
    public void Create_RoundsToCents()
    {
        Money.Create(10.005m).Value.Amount.ShouldBe(10.01m);
    }

    [Fact]
    public void Arithmetic_AddsAndMultiplies()
    {
        var price = Money.Create(12.50m).Value;

        (price * 3 + price).Amount.ShouldBe(50.00m);
    }
}
