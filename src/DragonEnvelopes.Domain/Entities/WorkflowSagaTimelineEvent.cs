namespace DragonEnvelopes.Domain.Entities;

public sealed class WorkflowSagaTimelineEvent
{
    public Guid Id { get; private set; }
    public Guid SagaId { get; private set; }
    public Guid? FamilyId { get; private set; }
    public string WorkflowType { get; private set; }
    public string Step { get; private set; }
    public string EventType { get; private set; }
    public string Status { get; private set; }
    public string? Message { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    public WorkflowSagaTimelineEvent(
        Guid id,
        Guid sagaId,
        Guid? familyId,
        string workflowType,
        string step,
        string eventType,
        string status,
        string? message,
        DateTimeOffset occurredAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Workflow saga timeline event id is required.");
        }

        if (sagaId == Guid.Empty)
        {
            throw new DomainValidationException("Workflow saga id is required.");
        }

        if (familyId.HasValue && familyId.Value == Guid.Empty)
        {
            throw new DomainValidationException("Family id cannot be empty.");
        }

        Id = id;
        SagaId = sagaId;
        FamilyId = familyId;
        WorkflowType = NormalizeRequired(workflowType, "Workflow type", 100);
        Step = NormalizeRequired(step, "Step", 160);
        EventType = NormalizeRequired(eventType, "Event type", 64);
        Status = NormalizeRequired(status, "Status", 32);
        Message = NormalizeOptional(message, 1000);
        OccurredAtUtc = occurredAtUtc;
    }

    private static string NormalizeRequired(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }
}
