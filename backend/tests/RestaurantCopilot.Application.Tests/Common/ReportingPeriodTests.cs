using RestaurantCopilot.Application.Common;
using RestaurantCopilot.Application.Tests.TestDoubles;

namespace RestaurantCopilot.Application.Tests.Common;

public sealed class ReportingPeriodTests
{
    [Fact]
    public void ToUtcRange_UsesRestaurantMidnights()
    {
        var period = new ReportingPeriod(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

        var (start, end) = period.ToUtcRange(TestData.Restaurant());

        // São Paulo is UTC-3 (no daylight saving time).
        start.ShouldBe(new DateTimeOffset(2026, 10, 1, 3, 0, 0, TimeSpan.Zero));
        end.ShouldBe(new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero));
        period.Days.ShouldBe(31);
    }

    [Fact]
    public void LastDays_EndsOnRestaurantLocalToday()
    {
        // 01:30 UTC on Nov 12 is still Nov 11 in São Paulo.
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 11, 12, 1, 30, 0, TimeSpan.Zero));

        var period = ReportingPeriod.LastDays(7, TestData.Restaurant(), timeProvider);

        period.ShouldBe(new ReportingPeriod(new DateOnly(2026, 11, 5), new DateOnly(2026, 11, 11)));
    }
}
