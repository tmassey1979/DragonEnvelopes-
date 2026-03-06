using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.Tests;

internal sealed class FakeOnboardingDataService : IOnboardingDataService
{
    public FakeOnboardingDataService(Guid familyId)
    {
        var now = DateTimeOffset.UtcNow;
        Profile = new OnboardingProfileData(
            Guid.NewGuid(),
            familyId,
            MembersCompleted: false,
            AccountsCompleted: false,
            EnvelopesCompleted: false,
            BudgetCompleted: false,
            PlaidCompleted: false,
            StripeAccountsCompleted: false,
            CardsCompleted: false,
            AutomationCompleted: false,
            IsCompleted: false,
            CreatedAtUtc: now,
            UpdatedAtUtc: now,
            CompletedAtUtc: null);

        BootstrapResult = new OnboardingBootstrapResultData(
            familyId,
            AccountsCreated: 1,
            EnvelopesCreated: 1,
            BudgetCreated: true);
    }

    public OnboardingProfileData Profile { get; set; }

    public OnboardingBootstrapResultData BootstrapResult { get; set; }

    public int UpdateProfileCallCount { get; private set; }

    public int BootstrapCallCount { get; private set; }

    public int ReconcileProfileCallCount { get; private set; }

    public Task<OnboardingProfileData> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Profile);
    }

    public Task<OnboardingProfileData> UpdateProfileAsync(
        bool membersCompleted,
        bool accountsCompleted,
        bool envelopesCompleted,
        bool budgetCompleted,
        bool plaidCompleted,
        bool stripeAccountsCompleted,
        bool cardsCompleted,
        bool automationCompleted,
        CancellationToken cancellationToken = default)
    {
        UpdateProfileCallCount += 1;
        var now = DateTimeOffset.UtcNow;
        var isCompleted = membersCompleted
                          && accountsCompleted
                          && envelopesCompleted
                          && budgetCompleted
                          && plaidCompleted
                          && stripeAccountsCompleted
                          && cardsCompleted
                          && automationCompleted;
        Profile = Profile with
        {
            MembersCompleted = membersCompleted,
            AccountsCompleted = accountsCompleted,
            EnvelopesCompleted = envelopesCompleted,
            BudgetCompleted = budgetCompleted,
            PlaidCompleted = plaidCompleted,
            StripeAccountsCompleted = stripeAccountsCompleted,
            CardsCompleted = cardsCompleted,
            AutomationCompleted = automationCompleted,
            IsCompleted = isCompleted,
            UpdatedAtUtc = now,
            CompletedAtUtc = isCompleted ? now : null
        };

        return Task.FromResult(Profile);
    }

    public Task<OnboardingProfileData> ReconcileProfileAsync(CancellationToken cancellationToken = default)
    {
        ReconcileProfileCallCount += 1;
        return Task.FromResult(Profile);
    }

    public Task<OnboardingBootstrapResultData> BootstrapAsync(
        IReadOnlyList<(string Name, string Type, decimal OpeningBalance)> accounts,
        IReadOnlyList<(string Name, decimal MonthlyBudget)> envelopes,
        (string Month, decimal TotalIncome)? budget,
        CancellationToken cancellationToken = default)
    {
        BootstrapCallCount += 1;
        return Task.FromResult(BootstrapResult);
    }
}
