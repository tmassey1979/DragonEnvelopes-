using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IPlaidSyncedTransactionRepository
{
    Task<bool> ExistsAsync(
        Guid familyId,
        string plaidTransactionId,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IReadOnlyCollection<PlaidSyncedTransaction> links,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
