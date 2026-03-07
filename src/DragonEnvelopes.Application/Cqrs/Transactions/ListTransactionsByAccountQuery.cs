using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Cqrs.Transactions;

public sealed record ListTransactionsByAccountQuery(Guid? AccountId) : IQuery<IReadOnlyList<TransactionDetails>>;
