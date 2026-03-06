namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record EnvelopeFinancialAccountItemViewModel(
    Guid Id,
    Guid EnvelopeId,
    string EnvelopeName,
    string Provider,
    string ProviderFinancialAccountId,
    string UpdatedAt);
