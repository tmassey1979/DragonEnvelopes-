namespace DragonEnvelopes.Contracts.Runtime;

public sealed record ApiVersionResponse(
    string Version,
    string Environment,
    DateTimeOffset UtcTime);
