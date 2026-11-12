using RestaurantCopilot.Infrastructure.Background;

namespace RestaurantCopilot.Infrastructure.Tests.Background;

public sealed class ReviewProcessingWorkerTests
{
    private static readonly TimeSpan Poll = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Max = TimeSpan.FromMinutes(15);

    [Theory]
    [InlineData(0, 30)]
    [InlineData(1, 60)]
    [InlineData(3, 240)]
    [InlineData(10, 900)]
    [InlineData(1000, 900)]
    public void NextDelay_DoublesOnEachFailureUpToTheMaximum(int failures, int expectedSeconds)
    {
        ReviewProcessingWorker.NextDelay(failures, Poll, Max).ShouldBe(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Fact]
    public async Task Trigger_CollapsesManyNotificationsIntoOneWakeUp()
    {
        using var trigger = new ReviewProcessingTrigger();

        trigger.Notify();
        trigger.Notify();
        trigger.Notify();

        (await trigger.WaitAsync(TimeSpan.Zero, TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await trigger.WaitAsync(TimeSpan.Zero, TestContext.Current.CancellationToken)).ShouldBeFalse();
    }
}
