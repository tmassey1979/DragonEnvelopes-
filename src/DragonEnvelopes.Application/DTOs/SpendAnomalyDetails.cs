namespace DragonEnvelopes.Application.DTOs;

public sealed record SpendAnomalyEventDetails(
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

public sealed record SpendAnomalySample(
    Guid TransactionId,
    string Merchant,
    decimal Amount,
    DateTimeOffset OccurredAt);
