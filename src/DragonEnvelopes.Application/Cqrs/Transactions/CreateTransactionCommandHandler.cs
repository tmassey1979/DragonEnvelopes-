using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain;

namespace DragonEnvelopes.Application.Cqrs.Transactions;

public sealed class CreateTransactionCommandHandler(
    ITransactionService transactionService,
    ITransactionRepository transactionRepository,
    IIntegrationEventPublisher integrationEventPublisher) : ICommandHandler<CreateTransactionCommand, TransactionDetails>
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

        var integrationEvent = new LedgerTransactionCreatedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredAtUtc: DateTimeOffset.UtcNow,
            FamilyId: familyId.Value,
            TransactionId: transaction.Id,
            AccountId: transaction.AccountId,
            Amount: transaction.Amount,
            Description: transaction.Description,
            Merchant: transaction.Merchant,
            Category: transaction.Category,
            EnvelopeId: transaction.EnvelopeId,
            IsSplit: transaction.Splits.Count > 0);

        await integrationEventPublisher.PublishAsync(
            IntegrationEventRoutingKeys.LedgerTransactionCreatedV1,
            integrationEvent,
            cancellationToken);

        return transaction;
    }
}
