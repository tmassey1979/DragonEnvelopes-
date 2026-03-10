namespace DragonEnvelopes.Contracts.Reports;

public sealed record ReportingProjectionReplayResponse(
    Guid? FamilyId,
    int ReplayedCount,
    int AppliedCount,
    int FailedCount,
    int EnvelopeProjectionRowCount,
    int TransactionProjectionRowCount,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc);
