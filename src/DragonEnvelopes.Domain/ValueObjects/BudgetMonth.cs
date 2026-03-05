namespace DragonEnvelopes.Domain.ValueObjects;

public readonly record struct BudgetMonth
{
    public int Year { get; }
    public int Month { get; }

    private BudgetMonth(int year, int month)
    {
        Year = year;
        Month = month;
    }

    public static BudgetMonth Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("Budget month is required.");
        }

        if (!DateOnly.TryParseExact($"{value}-01", "yyyy-MM-dd", out var date))
        {
            throw new DomainValidationException("Budget month must use yyyy-MM format.");
        }

        return new BudgetMonth(date.Year, date.Month);
    }

    public override string ToString() => $"{Year:0000}-{Month:00}";
}

