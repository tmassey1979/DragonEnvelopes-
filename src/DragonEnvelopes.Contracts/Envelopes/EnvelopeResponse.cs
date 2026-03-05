namespace DragonEnvelopes.Contracts.Envelopes;

public sealed record EnvelopeResponse(
    Guid Id,
    Guid FamilyId,
    string Name,
    decimal MonthlyBudget,
    decimal CurrentBalance,
    DateTimeOffset? LastActivityAt,
    bool IsArchived);

