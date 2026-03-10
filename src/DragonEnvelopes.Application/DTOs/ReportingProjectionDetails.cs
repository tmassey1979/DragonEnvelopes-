namespace DragonEnvelopes.Application.DTOs;

public sealed record ReportingProjectionBatchDetails(
    int LoadedCount,
    int AppliedCount,
    int FailedCount,
    int RemainingCount,
    DateTimeOffset ProcessedAtUtc);

public sealed record ReportingProjectionReplayDetails(
    Guid? FamilyId,
    int ReplayedCount,
    int AppliedCount,
    int FailedCount,
    int EnvelopeProjectionRowCount,
    int TransactionProjectionRowCount,
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
