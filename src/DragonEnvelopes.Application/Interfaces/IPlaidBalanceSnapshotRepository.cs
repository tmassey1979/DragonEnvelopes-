using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IPlaidBalanceSnapshotRepository
{
    Task AddRangeAsync(
        IReadOnlyCollection<PlaidBalanceSnapshot> snapshots,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlaidBalanceSnapshot>> ListRecentByFamilyAsync(
        Guid familyId,
        int take,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
