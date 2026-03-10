namespace DragonEnvelopes.Contracts.Reports;

public sealed record ReportingProjectionReplayRunResponse(
    Guid Id,
    Guid? FamilyId,
    string ProjectionSet,
    DateTimeOffset? FromOccurredAtUtc,
    DateTimeOffset? ToOccurredAtUtc,
    bool IsDryRun,
    bool ResetState,
    int BatchSize,
    int MaxEvents,
    int ThrottleMilliseconds,
    int TargetedEventCount,
    int ProcessedEventCount,
    int AppliedCount,
    int FailedCount,
    int BatchesProcessed,
    bool WasCappedByMaxEvents,
    string Status,
    string? RequestedByUserId,
    string? ErrorMessage,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc);
