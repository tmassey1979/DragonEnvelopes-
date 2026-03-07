namespace DragonEnvelopes.Desktop.Services;

public sealed record SystemRuntimeStatusData(
    string HealthStatus,
    string Version,
    string Environment,
    DateTimeOffset CheckedAtUtc,
    string FamilyApiHealthStatus = "Unknown",
    string LedgerApiHealthStatus = "Unknown",
    string FamilyApiStatusMessage = "Family API status not checked.",
    string LedgerApiStatusMessage = "Ledger API status not checked.");
