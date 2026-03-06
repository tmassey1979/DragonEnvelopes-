using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IPlaidTransactionSyncService
{
    Task<PlaidAccountLinkDetails> UpsertAccountLinkAsync(
        Guid familyId,
        Guid accountId,
        string plaidAccountId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlaidAccountLinkDetails>> ListAccountLinksAsync(
        Guid familyId,
        CancellationToken cancellationToken = default);

    Task<PlaidTransactionSyncDetails> SyncFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlaidTransactionSyncDetails>> SyncConnectedFamiliesAsync(
        CancellationToken cancellationToken = default);
}
