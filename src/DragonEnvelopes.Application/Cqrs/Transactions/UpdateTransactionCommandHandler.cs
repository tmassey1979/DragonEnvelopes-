using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;

namespace DragonEnvelopes.Application.Cqrs.Transactions;

public sealed class UpdateTransactionCommandHandler(
    ITransactionService transactionService) : ICommandHandler<UpdateTransactionCommand, TransactionDetails>
{
    public Task<TransactionDetails> HandleAsync(
        UpdateTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        return transactionService.UpdateAsync(
            command.TransactionId,
            command.Description,
            command.Merchant,
            command.Category,
            command.ReplaceAllocation,
            command.EnvelopeId,
            command.Splits,
            cancellationToken);
    }
}
