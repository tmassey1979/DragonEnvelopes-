using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IPlaidAccountLinkRepository
{
    Task<PlaidAccountLink?> GetByFamilyAndPlaidAccountIdAsync(
        Guid familyId,
        string plaidAccountId,
        CancellationToken cancellationToken = default);

    Task<PlaidAccountLink?> GetByFamilyAndPlaidAccountIdForUpdateAsync(
        Guid familyId,
        string plaidAccountId,
        CancellationToken cancellationToken = default);

    Task<PlaidAccountLink?> GetByFamilyAndIdForUpdateAsync(
        Guid familyId,
        Guid linkId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlaidAccountLink>> ListByFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default);

    Task AddAsync(PlaidAccountLink link, CancellationToken cancellationToken = default);

    Task DeleteAsync(PlaidAccountLink link, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
