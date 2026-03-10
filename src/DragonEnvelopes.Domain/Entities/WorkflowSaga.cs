namespace DragonEnvelopes.Domain.Entities;

public sealed class WorkflowSaga
{
    public Guid Id { get; private set; }
    public Guid? FamilyId { get; private set; }
    public string WorkflowType { get; private set; }
    public string CorrelationId { get; private set; }
    public string? ReferenceId { get; private set; }
    public string Status { get; private set; }
    public string CurrentStep { get; private set; }
    public string? FailureReason { get; private set; }
    public string? CompensationAction { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public WorkflowSaga(
        Guid id,
        Guid? familyId,
        string workflowType,
        string correlationId,
        string? referenceId,
        string status,
        string currentStep,
        string? failureReason,
        string? compensationAction,
        DateTimeOffset startedAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? completedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Workflow saga id is required.");
        }

        if (familyId.HasValue && familyId.Value == Guid.Empty)
        {
            throw new DomainValidationException("Family id cannot be empty.");
        }

        Id = id;
        FamilyId = familyId;
        WorkflowType = NormalizeRequired(workflowType, "Workflow type", 100);
        CorrelationId = NormalizeRequired(correlationId, "Correlation id", 256);
        ReferenceId = NormalizeOptional(referenceId, 256);
        Status = NormalizeRequired(status, "Status", 32);
        CurrentStep = NormalizeRequired(currentStep, "Current step", 160);
        FailureReason = NormalizeOptional(failureReason, 500);
        CompensationAction = NormalizeOptional(compensationAction, 500);
        StartedAtUtc = startedAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        CompletedAtUtc = completedAtUtc;
    }

    public void AssignFamily(Guid familyId, DateTimeOffset updatedAtUtc)
    {
        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (FamilyId.HasValue && FamilyId.Value != familyId)
        {
            throw new DomainValidationException("Saga is already associated with a different family.");
        }

        FamilyId = familyId;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Advance(
        string currentStep,
        string status,
        string? failureReason,
        string? compensationAction,
        DateTimeOffset updatedAtUtc,
        bool markCompleted)
    {
        CurrentStep = NormalizeRequired(currentStep, "Current step", 160);
        Status = NormalizeRequired(status, "Status", 32);
        FailureReason = NormalizeOptional(failureReason, 500);
        CompensationAction = NormalizeOptional(compensationAction, 500);
        UpdatedAtUtc = updatedAtUtc;

        if (markCompleted)
        {
            CompletedAtUtc = updatedAtUtc;
        }
        else if (!Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)
                 && !Status.Equals("Compensated", StringComparison.OrdinalIgnoreCase))
        {
            CompletedAtUtc = null;
        }
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
