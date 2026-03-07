using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Cqrs.Transactions;

public sealed record RestoreTransactionCommand(Guid TransactionId) : ICommand<TransactionDetails>;
