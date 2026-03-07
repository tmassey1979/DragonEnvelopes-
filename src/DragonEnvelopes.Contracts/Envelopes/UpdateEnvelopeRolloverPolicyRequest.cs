namespace DragonEnvelopes.Contracts.Envelopes;

public sealed record UpdateEnvelopeRolloverPolicyRequest(
    string RolloverMode,
    decimal? RolloverCap);
