namespace DragonEnvelopes.Contracts.Approvals;

public sealed record CreateApprovalRequestRequest(
    Guid FamilyId,
    Guid AccountId,
    decimal Amount,
    string Description,
    string Merchant,
    DateTimeOffset OccurredAt,
    string? Category,
    Guid? EnvelopeId,
    string? Notes);

public sealed record ResolveApprovalRequestRequest(
    string? Notes);

public sealed record ApprovalRequestResponse(
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
    string Status,
    string? RequestNotes,
    string? ResolutionNotes,
    string? ResolvedByUserId,
    string? ResolvedByRole,
    DateTimeOffset? ResolvedAtUtc,
    Guid? ApprovedTransactionId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
