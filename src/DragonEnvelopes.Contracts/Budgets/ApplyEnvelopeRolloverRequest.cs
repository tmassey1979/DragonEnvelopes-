namespace DragonEnvelopes.Contracts.Budgets;

public sealed record ApplyEnvelopeRolloverRequest(
    Guid FamilyId,
    string Month);
