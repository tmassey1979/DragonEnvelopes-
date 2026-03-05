namespace DragonEnvelopes.Application.DTOs;

public sealed record EnvelopeDetails(
    Guid Id,
    Guid FamilyId,
    string Name,
    decimal MonthlyBudget,
    decimal CurrentBalance,
    DateTimeOffset? LastActivityAt,
    bool IsArchived);
