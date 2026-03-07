namespace DragonEnvelopes.Contracts.Envelopes;

public sealed record CreateEnvelopeRequest(
    Guid FamilyId,
    string Name,
    decimal MonthlyBudget,
    string? RolloverMode = null,
    decimal? RolloverCap = null);
