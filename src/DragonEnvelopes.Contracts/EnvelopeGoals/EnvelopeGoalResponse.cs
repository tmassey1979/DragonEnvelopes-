namespace DragonEnvelopes.Contracts.EnvelopeGoals;

public sealed record EnvelopeGoalResponse(
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
