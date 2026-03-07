using DragonEnvelopes.Application.Cqrs.Messaging;

namespace DragonEnvelopes.Financial.Api.Services;

public interface ILedgerTransactionCreatedEventHandler
{
    Task HandleAsync(
        LedgerTransactionCreatedIntegrationEvent payload,
        CancellationToken cancellationToken = default);
}

public sealed class LedgerTransactionCreatedLoggingHandler(
    ILogger<LedgerTransactionCreatedLoggingHandler> logger) : ILedgerTransactionCreatedEventHandler
{
    public Task HandleAsync(
        LedgerTransactionCreatedIntegrationEvent payload,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Consumed ledger transaction event. EventId={EventId}, FamilyId={FamilyId}, TransactionId={TransactionId}, Amount={Amount}",
            payload.EventId,
            payload.FamilyId,
            payload.TransactionId,
            payload.Amount);
        return Task.CompletedTask;
    }
}
