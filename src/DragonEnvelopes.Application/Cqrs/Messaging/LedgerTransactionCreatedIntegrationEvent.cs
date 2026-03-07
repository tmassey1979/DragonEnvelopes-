namespace DragonEnvelopes.Application.Cqrs.Messaging;

public static class IntegrationEventRoutingKeys
{
    public const string LedgerTransactionCreatedV1 = "ledger.transaction.created.v1";
}

public sealed record LedgerTransactionCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    Guid TransactionId,
    Guid AccountId,
    decimal Amount,
    string Description,
    string Merchant,
    string? Category,
    Guid? EnvelopeId,
    bool IsSplit);
