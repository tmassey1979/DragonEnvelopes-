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
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionDetails>> ListAsync(
        Guid? accountId,
        CancellationToken cancellationToken = default);
}
