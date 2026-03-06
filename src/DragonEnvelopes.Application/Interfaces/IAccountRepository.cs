using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IAccountRepository
{
    Task AddAccountAsync(Account account, CancellationToken cancellationToken = default);

    Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<bool> AccountNameExistsAsync(
        Guid familyId,
        string name,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Account>> ListAccountsAsync(
        Guid? familyId,
        CancellationToken cancellationToken = default);

    Task<Account?> GetByIdForUpdateAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
