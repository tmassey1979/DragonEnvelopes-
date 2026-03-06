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
        var familyMembersDataService = new FakeFamilyMembersDataService();
        familyMembersDataService.Members.Add(new FamilyMemberItemViewModel(Guid.NewGuid(), "owner-1", "Owner One", "owner1@test.dev", "Parent"));
        familyMembersDataService.Members.Add(new FamilyMemberItemViewModel(Guid.NewGuid(), "owner-2", "Owner Two", "owner2@test.dev", "Adult"));

        var viewModel = new OnboardingWizardViewModel(onboardingDataService, familyMembersDataService);
        await EnsureLoadedAsync(viewModel);

        Assert.False(viewModel.HasError);
        Assert.Equal(3, viewModel.CurrentStepIndex);
        Assert.Equal("Envelopes", viewModel.CurrentStepTitle);
        Assert.Equal(33, viewModel.ProgressPercent);
        Assert.Equal(10, viewModel.StepItems.Count);
        Assert.True(viewModel.StepItems[0].IsCompleted);
        Assert.True(viewModel.StepItems[1].IsCompleted);
        Assert.True(viewModel.StepItems[2].IsCompleted);
        Assert.False(viewModel.StepItems[3].IsCompleted);
        Assert.True(viewModel.StepItems[3].IsCurrent);
        Assert.Equal("Current", viewModel.StepItems[3].StatusLabel);
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
        var familyMembersDataService = new FakeFamilyMembersDataService();
        familyMembersDataService.Members.Add(new FamilyMemberItemViewModel(Guid.NewGuid(), "owner-1", "Owner One", "owner1@test.dev", "Parent"));
        familyMembersDataService.Members.Add(new FamilyMemberItemViewModel(Guid.NewGuid(), "owner-2", "Owner Two", "owner2@test.dev", "Adult"));

        var viewModel = new OnboardingWizardViewModel(onboardingDataService, familyMembersDataService);
        await EnsureLoadedAsync(viewModel);

        Assert.Equal(5, viewModel.CurrentStepIndex);
        Assert.Equal("Plaid Connection", viewModel.CurrentStepTitle);

        await viewModel.MarkCurrentStepCompleteCommand.ExecuteAsync(null);
        await WaitForIdleAsync(viewModel);

        Assert.False(viewModel.HasError);
        Assert.Equal(1, onboardingDataService.UpdateProfileCallCount);
        Assert.True(viewModel.PlaidCompleted);
        Assert.Equal(6, viewModel.CurrentStepIndex);
        Assert.Equal("Progress saved.", viewModel.StatusMessage);
        Assert.Equal(67, viewModel.ProgressPercent);
        Assert.True(viewModel.StepItems[5].IsCompleted);
        Assert.False(viewModel.StepItems[5].IsCurrent);
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
        var familyMembersDataService = new FakeFamilyMembersDataService();
        familyMembersDataService.Members.Add(new FamilyMemberItemViewModel(Guid.NewGuid(), "owner-1", "Owner One", "owner1@test.dev", "Parent"));
        familyMembersDataService.Members.Add(new FamilyMemberItemViewModel(Guid.NewGuid(), "owner-2", "Owner Two", "owner2@test.dev", "Adult"));

        var viewModel = new OnboardingWizardViewModel(onboardingDataService, familyMembersDataService);
        await EnsureLoadedAsync(viewModel);

        await viewModel.ReconcileProgressCommand.ExecuteAsync(null);
        await WaitForIdleAsync(viewModel);

        Assert.False(viewModel.HasError);
        Assert.Equal(1, onboardingDataService.ReconcileProfileCallCount);
        Assert.Equal("Onboarding reconciled from current family data.", viewModel.StatusMessage);
        Assert.Equal(4, viewModel.CurrentStepIndex);
    }

    [Fact]
    public async Task SaveFamilyProfileCommand_Persists_Profile_And_Advances_To_Members_Step()
    {
        var familyId = Guid.Parse("10000000-0000-0000-0000-000000000004");
        var now = DateTimeOffset.UtcNow;
        var onboardingDataService = new FakeOnboardingDataService(familyId)
        {
            FamilyProfile = new FamilyProfileData(
                familyId,
                string.Empty,
                "USD",
                "America/Chicago",
                now,
                now)
        };

        var viewModel = new OnboardingWizardViewModel(onboardingDataService, new FakeFamilyMembersDataService());
        await EnsureLoadedAsync(viewModel);

        Assert.Equal(0, viewModel.CurrentStepIndex);
        Assert.False(viewModel.FamilyProfileCompleted);

        viewModel.FamilyNameDraft = "Household Prime";
        viewModel.SelectedCurrencyCode = "USD";
        viewModel.SelectedTimeZoneId = "America/Chicago";

        await viewModel.SaveFamilyProfileCommand.ExecuteAsync(null);
        await WaitForIdleAsync(viewModel);

        Assert.False(viewModel.HasError);
        Assert.True(viewModel.FamilyProfileCompleted);
        Assert.Equal(1, onboardingDataService.UpdateFamilyProfileCallCount);
        Assert.Equal(1, viewModel.CurrentStepIndex);
        Assert.Equal("Family profile saved.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task CreateInviteCommand_Updates_Members_Step_From_Real_State()
    {
        var familyId = Guid.Parse("10000000-0000-0000-0000-000000000005");
        var onboardingDataService = new FakeOnboardingDataService(familyId);
        var familyMembersDataService = new FakeFamilyMembersDataService();
        var viewModel = new OnboardingWizardViewModel(onboardingDataService, familyMembersDataService);
        await EnsureLoadedAsync(viewModel);

        Assert.Equal(1, viewModel.CurrentStepIndex);
        Assert.False(viewModel.MembersCompleted);

        viewModel.DraftInviteEmail = "invite@test.dev";
        viewModel.DraftInviteRole = "Adult";
        viewModel.DraftInviteExpiresInHours = "72";

        await viewModel.CreateInviteCommand.ExecuteAsync(null);
        await WaitForIdleAsync(viewModel);

        Assert.False(viewModel.HasError);
        Assert.True(viewModel.MembersCompleted);
        Assert.Equal(1, familyMembersDataService.CreateInviteCallCount);
        Assert.Equal(1, onboardingDataService.UpdateProfileCallCount);
        Assert.Equal("Invite created for 'invite@test.dev'.", viewModel.MemberStepMessage);
    }

    [Fact]
    public async Task MarkCurrentStepComplete_Saves_Budget_Preferences_And_Completes_Budget_Milestone()
    {
        var familyId = Guid.Parse("10000000-0000-0000-0000-000000000006");
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
                PlaidCompleted: false,
                StripeAccountsCompleted: false,
                CardsCompleted: false,
                AutomationCompleted: false,
                IsCompleted: false,
                CreatedAtUtc: now,
                UpdatedAtUtc: now,
                CompletedAtUtc: null)
        };

        var familyMembersDataService = new FakeFamilyMembersDataService();
        familyMembersDataService.Members.Add(new FamilyMemberItemViewModel(Guid.NewGuid(), "owner-1", "Owner One", "owner1@test.dev", "Parent"));
        familyMembersDataService.Members.Add(new FamilyMemberItemViewModel(Guid.NewGuid(), "owner-2", "Owner Two", "owner2@test.dev", "Adult"));

        var viewModel = new OnboardingWizardViewModel(onboardingDataService, familyMembersDataService);
        await EnsureLoadedAsync(viewModel);

        Assert.Equal(4, viewModel.CurrentStepIndex);

        viewModel.SelectedPayFrequency = "BiWeekly";
        viewModel.SelectedBudgetingStyle = "EnvelopePriority";
        viewModel.HouseholdMonthlyIncomeDraft = "7800.25";

        await viewModel.MarkCurrentStepCompleteCommand.ExecuteAsync(null);
        await WaitForIdleAsync(viewModel);

        Assert.False(viewModel.HasError);
        Assert.Equal(1, onboardingDataService.UpdateBudgetPreferencesCallCount);
        Assert.Equal(1, onboardingDataService.UpdateProfileCallCount);
        Assert.True(viewModel.BudgetCompleted);
        Assert.Equal(5, viewModel.CurrentStepIndex);
        Assert.Contains("BiWeekly", viewModel.BudgetPreferenceSummary);
        Assert.Contains("EnvelopePriority", viewModel.BudgetPreferenceSummary);
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
