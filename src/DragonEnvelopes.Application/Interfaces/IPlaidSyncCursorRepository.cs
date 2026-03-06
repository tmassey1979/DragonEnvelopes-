using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IPlaidSyncCursorRepository
{
    Task<PlaidSyncCursor?> GetByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<PlaidSyncCursor?> GetByFamilyIdForUpdateAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task AddAsync(PlaidSyncCursor cursor, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
