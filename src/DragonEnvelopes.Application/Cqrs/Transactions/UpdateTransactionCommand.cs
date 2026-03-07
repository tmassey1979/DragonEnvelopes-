using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Cqrs.Transactions;

public sealed record UpdateTransactionCommand(
    Guid TransactionId,
    string Description,
    string Merchant,
    string? Category,
    bool ReplaceAllocation,
    Guid? EnvelopeId,
    IReadOnlyList<TransactionSplitCreateDetails>? Splits) : ICommand<TransactionDetails>;
