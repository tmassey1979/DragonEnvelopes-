using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface ITransactionService
{
    Task<TransactionDetails> CreateAsync(
        Guid accountId,
        decimal amount,
        string description,
        string merchant,
        DateTimeOffset occurredAt,
        string? category,
        Guid? envelopeId,
        bool hasSplits,
        IReadOnlyList<TransactionSplitCreateDetails>? splits,
        CancellationToken cancellationToken = default);

    Task<TransactionDetails> UpdateAsync(
        Guid transactionId,
        string description,
        string merchant,
        string? category,
        bool replaceAllocation,
        Guid? envelopeId,
        IReadOnlyList<TransactionSplitCreateDetails>? splits,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionDetails>> ListAsync(
        Guid? accountId,
        CancellationToken cancellationToken = default);
}
