using System.Diagnostics;
using DragonEnvelopes.Desktop.Services;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Tests;

public sealed class OnboardingWizardViewModelTests
{
    [Fact]
    public async Task LoadCommand_Uses_First_Incomplete_Phase2_Step_And_Progress()
    {
        var familyId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var now = DateTimeOffset.UtcNow;
        var onboardingDataService = new FakeOnboardingDataService(familyId)
        {
            Profile = new OnboardingProfileData(
                Guid.NewGuid(),
                familyId,
                MembersCompleted: true,
                AccountsCompleted: true,
                EnvelopesCompleted: false,
                BudgetCompleted: false,
                PlaidCompleted: false,
                StripeAccountsCompleted: false,
                CardsCompleted: false,
                AutomationCompleted: false,
                IsCompleted: false,
                CreatedAtUtc: now,
                UpdatedAtUtc: now,
                CompletedAtUtc: null)
        };

        var viewModel = new OnboardingWizardViewModel(onboardingDataService);
        await EnsureLoadedAsync(viewModel);

        Assert.False(viewModel.HasError);
        Assert.Equal(2, viewModel.CurrentStepIndex);
        Assert.Equal("Envelopes", viewModel.CurrentStepTitle);
        Assert.Equal(25, viewModel.ProgressPercent);
        Assert.Equal(9, viewModel.StepItems.Count);
        Assert.True(viewModel.StepItems[0].IsCompleted);
        Assert.True(viewModel.StepItems[1].IsCompleted);
        Assert.False(viewModel.StepItems[2].IsCompleted);
        Assert.True(viewModel.StepItems[2].IsCurrent);
        Assert.Equal("Current", viewModel.StepItems[2].StatusLabel);
    }

    [Fact]
    public async Task MarkCurrentStepCompleteCommand_Completes_Current_Step_And_Advances()
    {
        var familyId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var now = DateTimeOffset.UtcNow;
        var onboardingDataService = new FakeOnboardingDataService(familyId)
        {
            Profile = new OnboardingProfileData(
                Guid.NewGuid(),
                familyId,
                MembersCompleted: true,
                AccountsCompleted: true,
                EnvelopesCompleted: true,
                BudgetCompleted: true,
                PlaidCompleted: false,
                StripeAccountsCompleted: false,
                CardsCompleted: false,
                AutomationCompleted: false,
                IsCompleted: false,
                CreatedAtUtc: now,
                UpdatedAtUtc: now,
                CompletedAtUtc: null)
        };

        var viewModel = new OnboardingWizardViewModel(onboardingDataService);
        await EnsureLoadedAsync(viewModel);

        Assert.Equal(4, viewModel.CurrentStepIndex);
        Assert.Equal("Plaid Connection", viewModel.CurrentStepTitle);

        await viewModel.MarkCurrentStepCompleteCommand.ExecuteAsync(null);
        await WaitForIdleAsync(viewModel);

        Assert.False(viewModel.HasError);
        Assert.Equal(1, onboardingDataService.UpdateProfileCallCount);
        Assert.True(viewModel.PlaidCompleted);
        Assert.Equal(6, viewModel.CurrentStepIndex);
        Assert.Equal("Progress saved.", viewModel.StatusMessage);
        Assert.Equal(63, viewModel.ProgressPercent);
        Assert.True(viewModel.StepItems[4].IsCompleted);
        Assert.False(viewModel.StepItems[4].IsCurrent);
        Assert.True(viewModel.StepItems[6].IsCurrent);
    }

    [Fact]
    public async Task ReconcileProgressCommand_Loads_Reconciled_Status()
    {
        var familyId = Guid.Parse("10000000-0000-0000-0000-000000000003");
        var now = DateTimeOffset.UtcNow;
        var onboardingDataService = new FakeOnboardingDataService(familyId)
        {
            Profile = new OnboardingProfileData(
                Guid.NewGuid(),
                familyId,
                MembersCompleted: true,
                AccountsCompleted: true,
                EnvelopesCompleted: true,
                BudgetCompleted: false,
                PlaidCompleted: true,
                StripeAccountsCompleted: false,
                CardsCompleted: false,
                AutomationCompleted: false,
                IsCompleted: false,
                CreatedAtUtc: now,
                UpdatedAtUtc: now,
                CompletedAtUtc: null)
        };

        var viewModel = new OnboardingWizardViewModel(onboardingDataService);
        await EnsureLoadedAsync(viewModel);

        await viewModel.ReconcileProgressCommand.ExecuteAsync(null);
        await WaitForIdleAsync(viewModel);

        Assert.False(viewModel.HasError);
        Assert.Equal(1, onboardingDataService.ReconcileProfileCallCount);
        Assert.Equal("Onboarding reconciled from current family data.", viewModel.StatusMessage);
        Assert.Equal(3, viewModel.CurrentStepIndex);
    }

    private static async Task EnsureLoadedAsync(OnboardingWizardViewModel viewModel)
    {
        await WaitForIdleAsync(viewModel);
        await viewModel.LoadCommand.ExecuteAsync(null);
        await WaitForIdleAsync(viewModel);
    }

    private static async Task WaitForIdleAsync(OnboardingWizardViewModel viewModel, int timeoutMilliseconds = 6000)
    {
        var stopwatch = Stopwatch.StartNew();
        while (viewModel.IsLoading)
        {
            if (stopwatch.ElapsedMilliseconds >= timeoutMilliseconds)
            {
                throw new TimeoutException("Timed out waiting for onboarding view model to become idle.");
            }

            await Task.Delay(20);
        }

        await Task.Delay(20);
    }
}
