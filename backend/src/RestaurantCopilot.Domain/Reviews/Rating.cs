using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Domain.Reviews;

/// <summary>Star rating from 1 to 5.</summary>
public readonly record struct Rating
{
    public const int Min = 1;
    public const int Max = 5;

    private Rating(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public static Result<Rating> Create(int value) =>
        value is < Min or > Max
            ? Error.Validation("Rating.OutOfRange", $"Rating must be between {Min} and {Max}.")
            : new Rating(value);
}
