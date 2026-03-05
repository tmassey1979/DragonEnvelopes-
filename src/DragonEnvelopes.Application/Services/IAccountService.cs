using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IAccountService
{
    Task<AccountDetails> CreateAsync(
        Guid familyId,
        string name,
        string type,
        decimal openingBalance,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountDetails>> ListAsync(
        Guid? familyId,
        CancellationToken cancellationToken = default);
}
