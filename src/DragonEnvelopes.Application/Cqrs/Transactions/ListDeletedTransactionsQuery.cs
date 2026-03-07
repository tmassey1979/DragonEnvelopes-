using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Cqrs.Transactions;

public sealed record ListDeletedTransactionsQuery(
    Guid FamilyId,
    int Days) : IQuery<IReadOnlyList<TransactionDetails>>;
