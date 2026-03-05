using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IRecurringBillRepository
{
    Task AddAsync(RecurringBill recurringBill, CancellationToken cancellationToken = default);

    Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecurringBill>> ListByFamilyAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<RecurringBill?> GetByIdForUpdateAsync(Guid recurringBillId, CancellationToken cancellationToken = default);

    Task DeleteAsync(RecurringBill recurringBill, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
