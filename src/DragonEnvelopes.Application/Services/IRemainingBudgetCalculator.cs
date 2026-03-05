using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IRemainingBudgetCalculator
{
    RemainingBudgetDetails Calculate(decimal totalIncome, IEnumerable<decimal> allocations);
}
