namespace DragonEnvelopes.Contracts.Approvals;

public sealed record ApprovalTimelineEventResponse(
    Guid Id,
    Guid FamilyId,
    Guid ApprovalRequestId,
    string EventType,
    string ActorUserId,
    string ActorRole,
    string Status,
    string? Notes,
    DateTimeOffset OccurredAtUtc);
