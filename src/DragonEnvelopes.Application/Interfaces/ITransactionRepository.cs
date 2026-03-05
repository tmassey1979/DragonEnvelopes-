using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface ITransactionRepository
{
    Task AddTransactionAsync(
        Transaction transaction,
        IReadOnlyList<TransactionSplitEntry> splitEntries,
        CancellationToken cancellationToken = default);

    Task<bool> AccountExistsAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> ListTransactionsAsync(
        Guid? accountId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionSplitEntry>> ListTransactionSplitsAsync(
        IReadOnlyCollection<Guid> transactionIds,
        CancellationToken cancellationToken = default);
}
