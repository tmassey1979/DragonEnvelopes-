namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record FamilyInviteTimelineItemViewModel(
    Guid Id,
    Guid InviteId,
    string Email,
    string EventType,
    string ActorUserId,
    DateTimeOffset OccurredAtUtc)
{
    public string OccurredAtUtcDisplay => OccurredAtUtc.ToString("yyyy-MM-dd HH:mm 'UTC'");
}
