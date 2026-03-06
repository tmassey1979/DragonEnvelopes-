namespace DragonEnvelopes.Application.DTOs;

public sealed record CardSpendEvaluationDetails(
    bool IsAllowed,
    string? DenialReason,
    decimal? RemainingDailyLimit);
