using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Cqrs.Transactions;

public sealed record CreateTransactionCommand(
    Guid AccountId,
    decimal Amount,
    string Description,
    string Merchant,
    DateTimeOffset OccurredAt,
    string? Category,
    Guid? EnvelopeId,
    bool HasSplits,
    IReadOnlyList<TransactionSplitCreateDetails>? Splits) : ICommand<TransactionDetails>;
