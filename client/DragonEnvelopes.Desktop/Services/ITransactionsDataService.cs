using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public interface ITransactionsDataService
{
    Task<IReadOnlyList<AccountListItemViewModel>> GetAccountsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnvelopeOptionViewModel>> GetEnvelopesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionListItemViewModel>> GetTransactionsAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionListItemViewModel>> GetDeletedTransactionsAsync(
        int days = 30,
        CancellationToken cancellationToken = default);

    Task CreateTransactionAsync(
        Guid accountId,
        decimal amount,
        string description,
        string merchant,
        DateTimeOffset occurredAt,
        string? category,
        Guid? envelopeId,
        IReadOnlyList<TransactionSplitDraftViewModel>? splits,
        CancellationToken cancellationToken = default);

    Task UpdateTransactionAsync(
        Guid transactionId,
        string description,
        string merchant,
        string? category,
        bool replaceAllocation,
        Guid? envelopeId,
        IReadOnlyList<TransactionSplitDraftViewModel>? splits,
        CancellationToken cancellationToken = default);

    Task DeleteTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);

    Task RestoreTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);
}
