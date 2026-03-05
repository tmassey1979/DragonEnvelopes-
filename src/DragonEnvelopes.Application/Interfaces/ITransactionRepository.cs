using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface ITransactionRepository
{
    Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default);

    Task<bool> AccountExistsAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task<bool> EnvelopeExistsAsync(Guid envelopeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> ListTransactionsAsync(
        Guid? accountId,
        CancellationToken cancellationToken = default);
}
