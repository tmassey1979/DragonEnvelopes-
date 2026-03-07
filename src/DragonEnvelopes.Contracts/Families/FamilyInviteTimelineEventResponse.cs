namespace DragonEnvelopes.Contracts.Families;

public sealed record FamilyInviteTimelineEventResponse(
    Guid Id,
    Guid FamilyId,
    Guid InviteId,
    string Email,
    string EventType,
    string? ActorUserId,
    DateTimeOffset OccurredAtUtc);
