namespace DragonEnvelopes.Contracts.Envelopes;

public sealed record EnvelopeResponse(
    Guid Id,
    Guid FamilyId,
    string Name,
    decimal MonthlyBudget,
    decimal CurrentBalance,
    string RolloverMode,
    decimal? RolloverCap,
    DateTimeOffset? LastActivityAt,
    bool IsArchived);
