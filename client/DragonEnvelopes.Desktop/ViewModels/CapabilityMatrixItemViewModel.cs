namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record CapabilityMatrixItemViewModel(
    string Domain,
    string Capability,
    string Status,
    string Notes);
