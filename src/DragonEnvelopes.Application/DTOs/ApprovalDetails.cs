using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.DTOs;

public sealed record ApprovalPolicyDetails(
    Guid Id,
    Guid FamilyId,
    bool IsEnabled,
    decimal AmountThreshold,
    IReadOnlyList<string> RolesRequiringApproval,
    DateTimeOffset UpdatedAtUtc);

public sealed record ApprovalRequestDetails(
    Guid Id,
    Guid FamilyId,
    Guid AccountId,
    string RequestedByUserId,
    string RequestedByRole,
    decimal Amount,
    string Description,
    string Merchant,
    DateTimeOffset OccurredAt,
    string? Category,
    Guid? EnvelopeId,
    PurchaseApprovalRequestStatus Status,
    string? RequestNotes,
    string? ResolutionNotes,
    string? ResolvedByUserId,
    string? ResolvedByRole,
    DateTimeOffset? ResolvedAtUtc,
    Guid? ApprovedTransactionId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ApprovalTimelineEventDetails(
    Guid Id,
    Guid FamilyId,
    Guid ApprovalRequestId,
    PurchaseApprovalTimelineEventType EventType,
    string ActorUserId,
    string ActorRole,
    string Status,
    string? Notes,
    DateTimeOffset OccurredAtUtc);
