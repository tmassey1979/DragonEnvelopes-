namespace DragonEnvelopes.Application.DTOs;

public sealed record FamilyInviteTimelineEventDetails(
    Guid Id,
    Guid FamilyId,
    Guid InviteId,
    string Email,
    string EventType,
    string? ActorUserId,
    DateTimeOffset OccurredAtUtc);
