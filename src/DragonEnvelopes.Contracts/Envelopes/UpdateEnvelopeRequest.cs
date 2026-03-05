namespace DragonEnvelopes.Contracts.Envelopes;

public sealed record UpdateEnvelopeRequest(
    string Name,
    decimal MonthlyBudget,
    bool IsArchived);

