namespace DragonEnvelopes.Application.Cqrs.Messaging;

public static class LedgerIntegrationEventNames
{
    public const string TransactionCreated = "TransactionCreated";
    public const string TransactionUpdated = "TransactionUpdated";
    public const string TransactionDeleted = "TransactionDeleted";
    public const string TransactionRestored = "TransactionRestored";
    public const string ApprovalRequestCreated = "ApprovalRequestCreated";
    public const string ApprovalRequestApproved = "ApprovalRequestApproved";
    public const string ApprovalRequestDenied = "ApprovalRequestDenied";
}

public sealed record TransactionUpdatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid TransactionId,
    Guid AccountId,
    decimal Amount,
    string Description,
    string Merchant,
    string? Category,
    Guid? EnvelopeId,
    bool IsSplit,
    bool ReplaceAllocation);

public sealed record TransactionDeletedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid TransactionId,
    Guid AccountId,
    decimal Amount,
    string? DeletedByUserId);

public sealed record TransactionRestoredIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid TransactionId,
    Guid AccountId,
    decimal Amount);

public sealed record ApprovalRequestCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid RequestId,
    Guid AccountId,
    string Status,
    decimal Amount,
    string RequestedByUserId,
    string RequestedByRole);

public sealed record ApprovalRequestApprovedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid RequestId,
    Guid AccountId,
    Guid ApprovedTransactionId,
    string ResolvedByUserId,
    string ResolvedByRole);

public sealed record ApprovalRequestDeniedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid RequestId,
    Guid AccountId,
    string ResolvedByUserId,
    string ResolvedByRole);
