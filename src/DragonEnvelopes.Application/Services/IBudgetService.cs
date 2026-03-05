using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IBudgetService
{
    Task<BudgetDetails> CreateAsync(
        Guid familyId,
        string month,
        decimal totalIncome,
        CancellationToken cancellationToken = default);

    Task<BudgetDetails?> GetByMonthAsync(
        Guid familyId,
        string month,
        CancellationToken cancellationToken = default);

    Task<BudgetDetails> UpdateAsync(
        Guid budgetId,
        decimal totalIncome,
        CancellationToken cancellationToken = default);
}
