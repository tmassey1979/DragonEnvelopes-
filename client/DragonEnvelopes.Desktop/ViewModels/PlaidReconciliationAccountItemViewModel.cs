namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record PlaidReconciliationAccountItemViewModel(
    Guid AccountId,
    string AccountName,
    string PlaidAccountId,
    string InternalBalance,
    string ProviderBalance,
    string DriftAmount,
    bool IsDrifted);
