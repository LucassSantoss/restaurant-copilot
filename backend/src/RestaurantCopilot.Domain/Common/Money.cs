namespace RestaurantCopilot.Domain.Common;

/// <summary>
/// A non-negative amount in Brazilian reais, rounded to cents. The application handles a
/// single currency, so the currency is implicit rather than stored.
/// </summary>
public readonly record struct Money
{
    private Money(decimal amount)
    {
        Amount = amount;
    }

    public decimal Amount { get; }

    public static Money Zero => new(0m);

    public static Result<Money> Create(decimal amount) =>
        amount < 0
            ? Error.Validation("Money.Negative", "Amount cannot be negative.")
            : new Money(decimal.Round(amount, 2, MidpointRounding.AwayFromZero));

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);

    public static Money operator *(Money money, int multiplier) => new(money.Amount * multiplier);

    public Money Add(Money other) => this + other;

    public Money Multiply(int multiplier) => this * multiplier;

    public override string ToString() => $"R$ {Amount:N2}";
}
