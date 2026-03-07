namespace DragonEnvelopes.Application.Cqrs.Transactions;

public sealed record DeleteTransactionCommand(
    Guid TransactionId,
    string? DeletedByUserId) : ICommand<bool>;
