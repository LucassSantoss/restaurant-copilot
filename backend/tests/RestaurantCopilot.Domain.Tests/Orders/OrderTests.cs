using RestaurantCopilot.Domain.Common;
using RestaurantCopilot.Domain.Orders;

namespace RestaurantCopilot.Domain.Tests.Orders;

public sealed class OrderTests
{
    private static readonly DateTimeOffset PlacedAt = new(2026, 10, 9, 20, 30, 0, TimeSpan.FromHours(-3));

    [Fact]
    public void Place_ComputesTotalAndStoresUtc()
    {
        OrderLine[] lines =
        [
            new(Guid.NewGuid(), 2, Money.Create(64.90m).Value),
            new(Guid.NewGuid(), 1, Money.Create(7.90m).Value),
        ];

        var order = Order.Place(1001, PlacedAt, OrderChannel.Delivery, lines).Value;

        order.Total.Amount.ShouldBe(137.70m);
        order.Items.Count.ShouldBe(2);
        order.Status.ShouldBe(OrderStatus.Completed);
        order.PlacedAt.Offset.ShouldBe(TimeSpan.Zero);
        order.PlacedAt.ShouldBe(PlacedAt);
    }

    [Fact]
    public void Place_WithoutItems_Fails()
    {
        var result = Order.Place(1001, PlacedAt, OrderChannel.Pickup, []);

        result.Error.Code.ShouldBe("Order.Items");
    }

    [Fact]
    public void Place_WithNonPositiveQuantity_Fails()
    {
        var result = Order.Place(1001, PlacedAt, OrderChannel.Pickup, [new(Guid.NewGuid(), 0, Money.Zero)]);

        result.Error.Code.ShouldBe("OrderItem.Quantity");
    }

    [Fact]
    public void Place_WithNonPositiveNumber_Fails()
    {
        var result = Order.Place(0, PlacedAt, OrderChannel.Pickup, [new(Guid.NewGuid(), 1, Money.Zero)]);

        result.Error.Code.ShouldBe("Order.Number");
    }

    [Fact]
    public void Cancel_Twice_FailsWithConflict()
    {
        var order = Order.Place(1001, PlacedAt, OrderChannel.DineIn, [new(Guid.NewGuid(), 1, Money.Zero)]).Value;

        order.Cancel().IsSuccess.ShouldBeTrue();
        var secondCancel = order.Cancel();

        order.Status.ShouldBe(OrderStatus.Cancelled);
        secondCancel.Error.Type.ShouldBe(ErrorType.Conflict);
    }
}
