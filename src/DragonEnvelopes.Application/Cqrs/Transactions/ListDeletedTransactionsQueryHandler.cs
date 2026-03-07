using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;

namespace DragonEnvelopes.Application.Cqrs.Transactions;

public sealed class ListDeletedTransactionsQueryHandler(
    ITransactionService transactionService) : IQueryHandler<ListDeletedTransactionsQuery, IReadOnlyList<TransactionDetails>>
{
    public Task<IReadOnlyList<TransactionDetails>> HandleAsync(
        ListDeletedTransactionsQuery query,
        CancellationToken cancellationToken = default)
    {
        return transactionService.ListDeletedAsync(
            query.FamilyId,
            query.Days,
            cancellationToken);
    }
}
