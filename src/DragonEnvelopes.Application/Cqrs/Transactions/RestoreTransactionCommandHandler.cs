using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;

namespace DragonEnvelopes.Application.Cqrs.Transactions;

public sealed class RestoreTransactionCommandHandler(
    ITransactionService transactionService) : ICommandHandler<RestoreTransactionCommand, TransactionDetails>
{
    public Task<TransactionDetails> HandleAsync(
        RestoreTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        return transactionService.RestoreAsync(command.TransactionId, cancellationToken);
    }
}
