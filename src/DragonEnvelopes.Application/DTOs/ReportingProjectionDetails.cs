namespace DragonEnvelopes.Application.DTOs;

public sealed record ReportingProjectionBatchDetails(
    int LoadedCount,
    int AppliedCount,
    int FailedCount,
    int RemainingCount,
    DateTimeOffset ProcessedAtUtc);

public sealed record ReportingProjectionReplayDetails(
    Guid ReplayRunId,
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
    int BatchesProcessed,
    bool WasCappedByMaxEvents,
    int ReplayedCount,
    int AppliedCount,
    int FailedCount,
    int EnvelopeProjectionRowCount,
    int TransactionProjectionRowCount,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    string Status,
    string? ErrorMessage);

public sealed record ReportingProjectionReplayRequestDetails(
    Guid? FamilyId,
    string ProjectionSet,
    DateTimeOffset? FromOccurredAtUtc,
    DateTimeOffset? ToOccurredAtUtc,
    bool IsDryRun,
    bool ResetState,
    int BatchSize,
    int MaxEvents,
    int ThrottleMilliseconds,
    string? RequestedByUserId);

public sealed record ReportingProjectionReplayRunDetails(
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

public sealed record ReportingProjectionStatusDetails(
    Guid? FamilyId,
    int PendingCount,
    int AppliedCount,
    int FailedCount,
    int EnvelopeProjectionRowCount,
    int TransactionProjectionRowCount,
    DateTimeOffset? LastAppliedAtUtc,
    DateTimeOffset? LatestEventOccurredAtUtc,
    decimal? ApproximateLagSeconds);
