using DragonEnvelopes.Application.Services;

namespace DragonEnvelopes.Application.Cqrs.Transactions;

public sealed class DeleteTransactionCommandHandler(
    ITransactionService transactionService) : ICommandHandler<DeleteTransactionCommand, bool>
{
    public async Task<bool> HandleAsync(
        DeleteTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        await transactionService.DeleteAsync(
            command.TransactionId,
            command.DeletedByUserId,
            cancellationToken);
        return true;
    }
}
