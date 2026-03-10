namespace DragonEnvelopes.Contracts.Reports;

public sealed record ReportingProjectionStatusResponse(
    Guid? FamilyId,
    int PendingCount,
    int AppliedCount,
    int FailedCount,
    int EnvelopeProjectionRowCount,
    int TransactionProjectionRowCount,
    DateTimeOffset? LastAppliedAtUtc,
    DateTimeOffset? LatestEventOccurredAtUtc,
    decimal? ApproximateLagSeconds);
