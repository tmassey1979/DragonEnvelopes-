namespace DragonEnvelopes.Contracts.Envelopes;

public sealed record UpdateEnvelopeRequest(
    string Name,
    decimal MonthlyBudget,
    bool IsArchived,
    string? RolloverMode = null,
    decimal? RolloverCap = null);
