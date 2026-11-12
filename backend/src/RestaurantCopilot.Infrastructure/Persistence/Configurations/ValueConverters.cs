using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RestaurantCopilot.Domain.Common;
using RestaurantCopilot.Domain.Reviews;

namespace RestaurantCopilot.Infrastructure.Persistence.Configurations;

internal static class ValueConverters
{
    // Values read from the database were validated when written, so .Value is safe here.
    public static readonly ValueConverter<Money, decimal> Money =
        new(money => money.Amount, amount => Domain.Common.Money.Create(amount).Value);

    public static readonly ValueConverter<Rating, int> Rating =
        new(rating => rating.Value, value => Domain.Reviews.Rating.Create(value).Value);
}
