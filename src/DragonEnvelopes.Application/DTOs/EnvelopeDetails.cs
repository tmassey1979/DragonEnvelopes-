namespace DragonEnvelopes.Application.DTOs;

public sealed record EnvelopeDetails(
    Guid Id,
    Guid FamilyId,
    string Name,
    decimal MonthlyBudget,
    decimal CurrentBalance,
    string RolloverMode,
    decimal? RolloverCap,
    DateTimeOffset? LastActivityAt,
    bool IsArchived);
