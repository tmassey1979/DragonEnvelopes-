namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record PlaidAccountLinkItemViewModel(
    Guid Id,
    Guid AccountId,
    string AccountName,
    string PlaidAccountId,
    string UpdatedAt);
