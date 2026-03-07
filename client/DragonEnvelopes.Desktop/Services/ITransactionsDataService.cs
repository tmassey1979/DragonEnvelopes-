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

    Task<CreateTransactionSubmissionResult> CreateTransactionAsync(
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

    Task CreateEnvelopeTransferAsync(
        Guid accountId,
        Guid fromEnvelopeId,
        Guid toEnvelopeId,
        decimal amount,
        DateTimeOffset occurredAt,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApprovalRequestItemViewModel>> GetApprovalRequestsAsync(
        string? status = null,
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<ApprovalRequestItemViewModel> ApproveApprovalRequestAsync(
        Guid requestId,
        string? notes = null,
        CancellationToken cancellationToken = default);

    Task<ApprovalRequestItemViewModel> DenyApprovalRequestAsync(
        Guid requestId,
        string? notes = null,
        CancellationToken cancellationToken = default);
}
