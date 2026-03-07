namespace DragonEnvelopes.Contracts.EnvelopeGoals;

public sealed record UpdateEnvelopeGoalRequest(
    decimal TargetAmount,
    DateOnly DueDate,
    string Status);
