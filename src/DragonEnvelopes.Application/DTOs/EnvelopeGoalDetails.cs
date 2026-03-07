namespace DragonEnvelopes.Application.DTOs;

public sealed record EnvelopeGoalDetails(
    Guid Id,
    Guid FamilyId,
    Guid EnvelopeId,
    string EnvelopeName,
    decimal CurrentBalance,
    decimal TargetAmount,
    DateOnly DueDate,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record EnvelopeGoalProjectionDetails(
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
