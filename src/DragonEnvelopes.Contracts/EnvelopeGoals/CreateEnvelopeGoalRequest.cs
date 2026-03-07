namespace DragonEnvelopes.Contracts.EnvelopeGoals;

public sealed record CreateEnvelopeGoalRequest(
    Guid FamilyId,
    Guid EnvelopeId,
    decimal TargetAmount,
    DateOnly DueDate,
    string Status);
