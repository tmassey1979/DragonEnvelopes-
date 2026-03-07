namespace DragonEnvelopes.Contracts.EnvelopeGoals;

public sealed record EnvelopeGoalProjectionResponse(
    Guid GoalId,
    Guid FamilyId,
    Guid EnvelopeId,
    string EnvelopeName,
    decimal CurrentBalance,
    decimal TargetAmount,
    DateOnly DueDate,
    string GoalStatus,
    decimal ProgressPercent,
    decimal ExpectedProgressPercent,
    decimal ExpectedBalance,
    decimal VarianceAmount,
    string ProjectionStatus);
