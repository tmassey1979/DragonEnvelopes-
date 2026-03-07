namespace DragonEnvelopes.Application.Cqrs.Messaging;

public static class IntegrationEventRoutingKeys
{
    public const string FamilyCreatedV1 = "family.family.created.v1";
    public const string FamilyMemberAddedV1 = "family.member.added.v1";
    public const string FamilyMemberRemovedV1 = "family.member.removed.v1";
    public const string FamilyInviteAcceptedV1 = "family.invite.accepted.v1";

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
