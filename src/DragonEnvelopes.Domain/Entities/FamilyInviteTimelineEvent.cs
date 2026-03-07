namespace DragonEnvelopes.Domain.Entities;

public sealed class FamilyInviteTimelineEvent
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public Guid InviteId { get; }
    public string Email { get; }
    public FamilyInviteTimelineEventType EventType { get; }
    public string? ActorUserId { get; }
    public DateTimeOffset OccurredAtUtc { get; }

    public FamilyInviteTimelineEvent(
        Guid id,
        Guid familyId,
        Guid inviteId,
        string email,
        FamilyInviteTimelineEventType eventType,
        string? actorUserId,
        DateTimeOffset occurredAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Invite timeline event id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (inviteId == Guid.Empty)
        {
            throw new DomainValidationException("Invite id is required.");
        }

        Id = id;
        FamilyId = familyId;
        InviteId = inviteId;
        Email = NormalizeEmail(email);
        EventType = eventType;
        ActorUserId = NormalizeOptional(actorUserId);
        OccurredAtUtc = occurredAtUtc;
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainValidationException("Invite event email is required.");
        }

        var normalized = email.Trim().ToLowerInvariant();
        if (!normalized.Contains('@'))
        {
            throw new DomainValidationException("Invite event email format is invalid.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
