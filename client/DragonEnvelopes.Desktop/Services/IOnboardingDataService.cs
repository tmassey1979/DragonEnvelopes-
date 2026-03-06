namespace DragonEnvelopes.Desktop.Services;

public interface IOnboardingDataService
{
    Task<OnboardingProfileData> GetProfileAsync(CancellationToken cancellationToken = default);

    Task<OnboardingProfileData> UpdateProfileAsync(
        bool membersCompleted,
        bool accountsCompleted,
        bool envelopesCompleted,
        bool budgetCompleted,
        bool plaidCompleted,
        bool stripeAccountsCompleted,
        bool cardsCompleted,
        bool automationCompleted,
        CancellationToken cancellationToken = default);

    Task<OnboardingProfileData> ReconcileProfileAsync(CancellationToken cancellationToken = default);

    Task<OnboardingBootstrapResultData> BootstrapAsync(
        IReadOnlyList<(string Name, string Type, decimal OpeningBalance)> accounts,
        IReadOnlyList<(string Name, decimal MonthlyBudget)> envelopes,
        (string Month, decimal TotalIncome)? budget,
        CancellationToken cancellationToken = default);
}
