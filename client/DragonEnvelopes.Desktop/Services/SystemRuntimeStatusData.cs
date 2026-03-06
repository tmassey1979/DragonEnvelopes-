namespace DragonEnvelopes.Desktop.Services;

public sealed record SystemRuntimeStatusData(
    string HealthStatus,
    string Version,
    string Environment,
    DateTimeOffset CheckedAtUtc);
