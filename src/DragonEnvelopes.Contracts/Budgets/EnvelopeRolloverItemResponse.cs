namespace DragonEnvelopes.Contracts.Budgets;

public sealed record EnvelopeRolloverItemResponse(
    Guid EnvelopeId,
    string EnvelopeName,
    decimal CurrentBalance,
    string RolloverMode,
    decimal? RolloverCap,
    decimal RolloverBalance,
    decimal AdjustmentAmount);
