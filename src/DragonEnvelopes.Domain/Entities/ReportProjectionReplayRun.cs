namespace DragonEnvelopes.Domain.Entities;

public sealed class ReportProjectionReplayRun
{
    public Guid Id { get; private set; }
    public Guid? FamilyId { get; private set; }
    public string ProjectionSet { get; private set; }
    public DateTimeOffset? FromOccurredAtUtc { get; private set; }
    public DateTimeOffset? ToOccurredAtUtc { get; private set; }
    public bool IsDryRun { get; private set; }
    public bool ResetState { get; private set; }
    public int BatchSize { get; private set; }
    public int MaxEvents { get; private set; }
    public int ThrottleMilliseconds { get; private set; }
    public int TargetedEventCount { get; private set; }
    public int ProcessedEventCount { get; private set; }
    public int AppliedCount { get; private set; }
    public int FailedCount { get; private set; }
    public int BatchesProcessed { get; private set; }
    public bool WasCappedByMaxEvents { get; private set; }
    public string Status { get; private set; }
    public string? RequestedByUserId { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset CompletedAtUtc { get; private set; }

    public ReportProjectionReplayRun(
        Guid id,
        Guid? familyId,
        string projectionSet,
        DateTimeOffset? fromOccurredAtUtc,
        DateTimeOffset? toOccurredAtUtc,
        bool isDryRun,
        bool resetState,
        int batchSize,
        int maxEvents,
        int throttleMilliseconds,
        int targetedEventCount,
        bool wasCappedByMaxEvents,
        string status,
        string? requestedByUserId,
        string? errorMessage,
        DateTimeOffset startedAtUtc,
        DateTimeOffset completedAtUtc,
        int processedEventCount = 0,
        int appliedCount = 0,
        int failedCount = 0,
        int batchesProcessed = 0)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Replay run id is required.");
        }

        if (familyId.HasValue && familyId.Value == Guid.Empty)
        {
            throw new DomainValidationException("Family id cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(projectionSet))
        {
            throw new DomainValidationException("Projection set is required.");
        }

        if (batchSize <= 0)
        {
            throw new DomainValidationException("Batch size must be positive.");
        }

        if (maxEvents <= 0)
        {
            throw new DomainValidationException("Max events must be positive.");
        }

        if (throttleMilliseconds < 0)
        {
            throw new DomainValidationException("Throttle milliseconds cannot be negative.");
        }

        if (targetedEventCount < 0)
        {
            throw new DomainValidationException("Targeted event count cannot be negative.");
        }

        if (processedEventCount < 0 || appliedCount < 0 || failedCount < 0 || batchesProcessed < 0)
        {
            throw new DomainValidationException("Replay counters cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new DomainValidationException("Replay status is required.");
        }

        Id = id;
        FamilyId = familyId;
        ProjectionSet = projectionSet.Trim();
        FromOccurredAtUtc = fromOccurredAtUtc;
        ToOccurredAtUtc = toOccurredAtUtc;
        IsDryRun = isDryRun;
        ResetState = resetState;
        BatchSize = batchSize;
        MaxEvents = maxEvents;
        ThrottleMilliseconds = throttleMilliseconds;
        TargetedEventCount = targetedEventCount;
        ProcessedEventCount = processedEventCount;
        AppliedCount = appliedCount;
        FailedCount = failedCount;
        BatchesProcessed = batchesProcessed;
        WasCappedByMaxEvents = wasCappedByMaxEvents;
        Status = status.Trim();
        RequestedByUserId = NormalizeOptional(requestedByUserId, 128);
        ErrorMessage = NormalizeOptional(errorMessage, 1000);
        StartedAtUtc = startedAtUtc;
        CompletedAtUtc = completedAtUtc;
    }

    public void Complete(
        int processedEventCount,
        int appliedCount,
        int failedCount,
        int batchesProcessed,
        DateTimeOffset completedAtUtc)
    {
        if (processedEventCount < 0 || appliedCount < 0 || failedCount < 0 || batchesProcessed < 0)
        {
            throw new DomainValidationException("Replay counters cannot be negative.");
        }

        ProcessedEventCount = processedEventCount;
        AppliedCount = appliedCount;
        FailedCount = failedCount;
        BatchesProcessed = batchesProcessed;
        Status = "Completed";
        ErrorMessage = null;
        CompletedAtUtc = completedAtUtc;
    }

    public void Fail(
        string errorMessage,
        int processedEventCount,
        int appliedCount,
        int failedCount,
        int batchesProcessed,
        DateTimeOffset completedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new DomainValidationException("Replay failure message is required.");
        }

        ProcessedEventCount = Math.Max(0, processedEventCount);
        AppliedCount = Math.Max(0, appliedCount);
        FailedCount = Math.Max(0, failedCount);
        BatchesProcessed = Math.Max(0, batchesProcessed);
        Status = "Failed";
        ErrorMessage = NormalizeOptional(errorMessage, 1000);
        CompletedAtUtc = completedAtUtc;
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
