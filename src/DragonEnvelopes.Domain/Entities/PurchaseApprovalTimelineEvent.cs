namespace DragonEnvelopes.Domain.Entities;

public sealed class PurchaseApprovalTimelineEvent
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public Guid ApprovalRequestId { get; }
    public PurchaseApprovalTimelineEventType EventType { get; }
    public string ActorUserId { get; }
    public string ActorRole { get; }
    public string Status { get; }
    public string? Notes { get; }
    public DateTimeOffset OccurredAtUtc { get; }

    public PurchaseApprovalTimelineEvent(
        Guid id,
        Guid familyId,
        Guid approvalRequestId,
        PurchaseApprovalTimelineEventType eventType,
        string actorUserId,
        string actorRole,
        string status,
        string? notes,
        DateTimeOffset occurredAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Approval timeline event id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (approvalRequestId == Guid.Empty)
        {
            throw new DomainValidationException("Approval request id is required.");
        }

        Id = id;
        FamilyId = familyId;
        ApprovalRequestId = approvalRequestId;
        EventType = eventType;
        ActorUserId = NormalizeRequired(actorUserId, "Actor user id");
        ActorRole = NormalizeRequired(actorRole, "Actor role");
        Status = NormalizeRequired(status, "Status");
        Notes = NormalizeOptional(notes);
        OccurredAtUtc = occurredAtUtc;
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
