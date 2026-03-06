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
        Assert.Equal(5, viewModel.CurrentStepIndex);
        Assert.Equal("Progress saved.", viewModel.StatusMessage);
        Assert.Equal(63, viewModel.ProgressPercent);
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
