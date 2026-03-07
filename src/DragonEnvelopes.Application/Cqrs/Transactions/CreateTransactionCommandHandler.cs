using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain;

namespace DragonEnvelopes.Application.Cqrs.Transactions;

public sealed class CreateTransactionCommandHandler(
    ITransactionService transactionService,
    ITransactionRepository transactionRepository) : ICommandHandler<CreateTransactionCommand, TransactionDetails>
{
    public async Task<TransactionDetails> HandleAsync(
        CreateTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        var familyId = await transactionRepository.GetAccountFamilyIdAsync(command.AccountId, cancellationToken);
        if (!familyId.HasValue)
        {
            throw new DomainValidationException("Account was not found.");
        }

        var transaction = await transactionService.CreateAsync(
            command.AccountId,
            command.Amount,
            command.Description,
            command.Merchant,
            command.OccurredAt,
            command.Category,
            command.EnvelopeId,
            command.HasSplits,
            command.Splits,
            cancellationToken);

        return transaction;
    }
}
