namespace DragonEnvelopes.Domain.ValueObjects;

public static class EnvelopeRolloverCalculator
{
    public static decimal Calculate(decimal currentBalance, EnvelopeRolloverMode rolloverMode, decimal? rolloverCap)
    {
        var normalizedBalance = Money.FromDecimal(currentBalance).Amount;
        if (normalizedBalance <= 0m)
        {
            return 0m;
        }

        var result = rolloverMode switch
        {
            EnvelopeRolloverMode.None => 0m,
            EnvelopeRolloverMode.Full => normalizedBalance,
            EnvelopeRolloverMode.Cap => Math.Min(normalizedBalance, NormalizeCap(rolloverCap)),
            _ => throw new DomainValidationException("Envelope rollover mode is invalid.")
        };

        return Money.FromDecimal(result).Amount;
    }

    private static decimal NormalizeCap(decimal? rolloverCap)
    {
        if (!rolloverCap.HasValue)
        {
            return 0m;
        }

        return Money.FromDecimal(Math.Max(rolloverCap.Value, 0m)).Amount;
    }
}
