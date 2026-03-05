using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public sealed class RemainingBudgetCalculator : IRemainingBudgetCalculator
{
    public RemainingBudgetDetails Calculate(decimal totalIncome, IEnumerable<decimal> allocations)
    {
        var normalizedIncome = decimal.Round(Math.Max(totalIncome, 0m), 2, MidpointRounding.AwayFromZero);
        var normalizedAllocations = allocations
            .Select(static amount => decimal.Round(Math.Max(amount, 0m), 2, MidpointRounding.AwayFromZero))
            .ToArray();

        var allocated = normalizedAllocations.Sum();
        var remaining = decimal.Round(normalizedIncome - allocated, 2, MidpointRounding.AwayFromZero);

        return new RemainingBudgetDetails(
            normalizedIncome,
            allocated,
            remaining);
    }
}
