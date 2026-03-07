using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface ITransactionRepository
{
    Task AddTransactionAsync(
        Transaction transaction,
        IReadOnlyList<TransactionSplitEntry> splitEntries,
        CancellationToken cancellationToken = default);

    Task<bool> AccountExistsAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task<Guid?> GetAccountFamilyIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task<bool> AccountBelongsToFamilyAsync(
        Guid accountId,
        Guid familyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> ListTransactionsAsync(
        Guid? accountId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> ListDeletedTransactionsByFamilyAsync(
        Guid familyId,
        DateTimeOffset deletedSinceUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionSplitEntry>> ListTransactionSplitsAsync(
        IReadOnlyCollection<Guid> transactionIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionSplitEntry>> ListTransactionSplitsByTransactionIdAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);

    Task ReplaceTransactionSplitsAsync(
        Guid transactionId,
        IReadOnlyList<TransactionSplitEntry> splitEntries,
        CancellationToken cancellationToken = default);

    Task<Transaction?> GetTransactionByIdForUpdateAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetTransactionFamilyIdAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task AddTransactionsAsync(
        IReadOnlyList<Transaction> transactions,
        CancellationToken cancellationToken = default);

    Task DeleteTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);
}
