using RestaurantCopilot.Domain.Restaurants;

namespace RestaurantCopilot.Application.Common;

/// <summary>
/// An inclusive range of calendar days in the restaurant's time zone. "Last week" for a
/// restaurant in São Paulo means São Paulo days, not UTC days.
/// </summary>
public sealed record ReportingPeriod(DateOnly From, DateOnly To)
{
    public int Days => To.DayNumber - From.DayNumber + 1;

    /// <summary>The <paramref name="days"/> days ending today (inclusive) in the restaurant's time zone.</summary>
    public static ReportingPeriod LastDays(int days, Restaurant restaurant, TimeProvider timeProvider)
    {
        var today = Today(restaurant, timeProvider);
        return new ReportingPeriod(today.AddDays(1 - days), today);
    }

    public static DateOnly Today(Restaurant restaurant, TimeProvider timeProvider) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), TimeZoneOf(restaurant)).DateTime);

    /// <summary>The period as a half-open UTC interval [start, end) for database queries.</summary>
    public (DateTimeOffset StartUtc, DateTimeOffset EndUtc) ToUtcRange(Restaurant restaurant)
    {
        var timeZone = TimeZoneOf(restaurant);
        return (StartOfDayUtc(From, timeZone), StartOfDayUtc(To.AddDays(1), timeZone));
    }

    private static DateTimeOffset StartOfDayUtc(DateOnly day, TimeZoneInfo timeZone)
    {
        var localMidnight = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(localMidnight, timeZone.GetUtcOffset(localMidnight)).ToUniversalTime();
    }

    private static TimeZoneInfo TimeZoneOf(Restaurant restaurant) => TimeZoneInfo.FindSystemTimeZoneById(restaurant.TimeZoneId);
}
