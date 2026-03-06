namespace DragonEnvelopes.Contracts.Runtime;

public sealed record ApiHealthResponse(
    string Status,
    DateTimeOffset UtcTime);
