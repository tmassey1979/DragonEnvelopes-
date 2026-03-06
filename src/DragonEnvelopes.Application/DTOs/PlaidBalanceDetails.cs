namespace DragonEnvelopes.Application.DTOs;

public sealed record PlaidBalanceRefreshDetails(
    Guid FamilyId,
    int RefreshedCount,
    int DriftedCount,
    decimal TotalAbsoluteDrift,
    DateTimeOffset RefreshedAtUtc);

public sealed record PlaidAccountDriftDetails(
    Guid AccountId,
    string AccountName,
    string PlaidAccountId,
    decimal InternalBalance,
    decimal ProviderBalance,
    decimal DriftAmount,
    bool IsDrifted);

public sealed record PlaidReconciliationReportDetails(
    Guid FamilyId,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<PlaidAccountDriftDetails> Accounts);
