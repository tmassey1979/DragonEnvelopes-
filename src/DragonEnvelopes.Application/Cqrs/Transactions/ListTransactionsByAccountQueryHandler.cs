using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;

namespace DragonEnvelopes.Application.Cqrs.Transactions;

public sealed class ListTransactionsByAccountQueryHandler(
    ITransactionService transactionService) : IQueryHandler<ListTransactionsByAccountQuery, IReadOnlyList<TransactionDetails>>
{
    public Task<IReadOnlyList<TransactionDetails>> HandleAsync(
        ListTransactionsByAccountQuery query,
        CancellationToken cancellationToken = default)
    {
        return transactionService.ListAsync(query.AccountId, cancellationToken);
    }
}
