using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Interfaces;

public interface IBudgetRepository
{
    Task AddAsync(Budget budget, CancellationToken cancellationToken = default);

    Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<bool> ExistsForMonthAsync(
        Guid familyId,
        BudgetMonth month,
        CancellationToken cancellationToken = default);

    Task<Budget?> GetByFamilyAndMonthAsync(
        Guid familyId,
        BudgetMonth month,
        CancellationToken cancellationToken = default);

    Task<Budget?> GetByIdForUpdateAsync(Guid budgetId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
