namespace DragonEnvelopes.Application.DTOs;

public sealed record EnvelopeRolloverItemDetails(
    Guid EnvelopeId,
    string EnvelopeName,
    decimal CurrentBalance,
    string RolloverMode,
    decimal? RolloverCap,
    decimal RolloverBalance,
    decimal AdjustmentAmount);
