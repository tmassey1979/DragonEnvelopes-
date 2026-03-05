using System.Globalization;

namespace DragonEnvelopes.Domain.ValueObjects;

public readonly record struct Money(decimal Amount)
{
    public static Money Zero => new(0m);

    public static Money FromDecimal(decimal amount) => new(decimal.Round(amount, 2, MidpointRounding.AwayFromZero));

    public Money EnsureNonNegative(string fieldName)
    {
        if (Amount < 0m)
        {
            throw new DomainValidationException($"{fieldName} cannot be negative.");
        }

        return this;
    }

    public bool IsZero => Amount == 0m;

    public override string ToString() => Amount.ToString("0.00", CultureInfo.InvariantCulture);

    public static Money operator +(Money left, Money right) => FromDecimal(left.Amount + right.Amount);

    public static Money operator -(Money left, Money right) => FromDecimal(left.Amount - right.Amount);

    public static bool operator >(Money left, Money right) => left.Amount > right.Amount;

    public static bool operator <(Money left, Money right) => left.Amount < right.Amount;

    public static bool operator >=(Money left, Money right) => left.Amount >= right.Amount;

    public static bool operator <=(Money left, Money right) => left.Amount <= right.Amount;
}

