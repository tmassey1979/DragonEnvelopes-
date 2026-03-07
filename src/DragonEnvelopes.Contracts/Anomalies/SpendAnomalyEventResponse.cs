namespace DragonEnvelopes.Contracts.Anomalies;

public sealed record SpendAnomalyEventResponse(
    Guid Id,
    Guid FamilyId,
    Guid TransactionId,
    Guid AccountId,
    string Merchant,
    decimal Amount,
    decimal BaselineAverageAmount,
    decimal BaselineStandardDeviation,
    int BaselineSampleSize,
    decimal DeviationRatio,
    int SeverityScore,
    string Reason,
    DateTimeOffset DetectedAtUtc);
