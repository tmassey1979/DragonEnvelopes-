namespace DragonEnvelopes.Application.Services;

public interface IPlaidGateway
{
    Task<(string LinkToken, DateTimeOffset ExpiresAtUtc)> CreateLinkTokenAsync(
        Guid familyId,
        string clientUserId,
        string clientName,
        CancellationToken cancellationToken = default);

    Task<(string ItemId, string AccessToken)> ExchangePublicTokenAsync(
        string publicToken,
        CancellationToken cancellationToken = default);

    Task<PlaidTransactionSyncResult> SyncTransactionsAsync(
        string accessToken,
        string? cursor,
        int count,
        CancellationToken cancellationToken = default);
}
