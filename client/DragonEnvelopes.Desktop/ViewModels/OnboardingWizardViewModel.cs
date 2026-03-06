using System.Collections.ObjectModel;
using System.Net.Mail;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class OnboardingWizardViewModel : ObservableObject
{
    private const string DefaultCurrencyCode = "USD";
    private const string DefaultTimeZoneId = "America/Chicago";

    private static readonly string[] StepTitles =
    [
        "Family Profile",
        "Family Members",
        "Accounts",
        "Envelopes",
        "Starter Budget",
        "Plaid Connection",
        "Stripe Accounts",
        "Cards",
        "Automation Rules",
        "Review"
    ];

    private static readonly string[] AccountTypes = ["Checking", "Savings", "Cash", "Credit"];
    private static readonly string[] CurrencyCodes = ["USD", "CAD", "EUR", "GBP"];
    private static readonly string[] TimeZoneIds =
    [
        "America/Chicago",
        "America/New_York",
        "America/Denver",
        "America/Los_Angeles",
        "UTC"
    ];

    private static readonly string[] InviteRoles = ["Parent", "Adult", "Teen", "Child"];
    private const int MilestoneCount = 9;

    private readonly IOnboardingDataService _onboardingDataService;
    private readonly IFamilyMembersDataService _familyMembersDataService;

    public OnboardingWizardViewModel(
        IOnboardingDataService onboardingDataService,
        IFamilyMembersDataService familyMembersDataService)
    {
        _onboardingDataService = onboardingDataService;
        _familyMembersDataService = familyMembersDataService;

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        NextStepCommand = new RelayCommand(NextStep);
        PreviousStepCommand = new RelayCommand(PreviousStep);
        MarkCurrentStepCompleteCommand = new AsyncRelayCommand(MarkCurrentStepCompleteAsync);
        ReconcileProgressCommand = new AsyncRelayCommand(ReconcileProgressAsync);
        SaveFamilyProfileCommand = new AsyncRelayCommand(SaveFamilyProfileAsync);
        CreateInviteCommand = new AsyncRelayCommand(CreateFamilyInviteAsync);
        CancelInviteCommand = new AsyncRelayCommand(CancelFamilyInviteAsync);
        AddAccountRowCommand = new RelayCommand(AddAccountRow);
        RemoveAccountRowCommand = new RelayCommand<OnboardingAccountDraftViewModel?>(RemoveAccountRow);
        AddEnvelopeRowCommand = new RelayCommand(AddEnvelopeRow);
        RemoveEnvelopeRowCommand = new RelayCommand<OnboardingEnvelopeDraftViewModel?>(RemoveEnvelopeRow);
        SubmitBootstrapCommand = new AsyncRelayCommand(SubmitBootstrapAsync);
        CancelCommand = new RelayCommand(Cancel);
        foreach (var (title, index) in StepTitles.Select(static (title, index) => (title, index)))
        {
            StepItems.Add(new OnboardingStepItemViewModel(index, title));
        }

        AddAccountRow();
        AddEnvelopeRow();
        BudgetMonth = DateTime.UtcNow.ToString("yyyy-MM");
        BudgetIncome = "0";
        SelectedCurrencyCode = DefaultCurrencyCode;
        SelectedTimeZoneId = DefaultTimeZoneId;
        DraftInviteRole = InviteRoles[0];
        DraftInviteExpiresInHours = "168";
        RefreshStepItems();
        _ = LoadCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadCommand { get; }
    public IRelayCommand NextStepCommand { get; }
    public IRelayCommand PreviousStepCommand { get; }
    public IAsyncRelayCommand MarkCurrentStepCompleteCommand { get; }
    public IAsyncRelayCommand ReconcileProgressCommand { get; }
    public IAsyncRelayCommand SaveFamilyProfileCommand { get; }
    public IAsyncRelayCommand CreateInviteCommand { get; }
    public IAsyncRelayCommand CancelInviteCommand { get; }
    public IRelayCommand AddAccountRowCommand { get; }
    public IRelayCommand<OnboardingAccountDraftViewModel?> RemoveAccountRowCommand { get; }
    public IRelayCommand AddEnvelopeRowCommand { get; }
    public IRelayCommand<OnboardingEnvelopeDraftViewModel?> RemoveEnvelopeRowCommand { get; }
    public IAsyncRelayCommand SubmitBootstrapCommand { get; }
    public IRelayCommand CancelCommand { get; }

    public IReadOnlyList<string> Steps { get; } = StepTitles;
    public IReadOnlyList<string> AccountTypeOptions { get; } = AccountTypes;
    public IReadOnlyList<string> CurrencyOptions { get; } = CurrencyCodes;
    public IReadOnlyList<string> TimeZoneOptions { get; } = TimeZoneIds;
    public IReadOnlyList<string> InviteRoleOptions { get; } = InviteRoles;

    [ObservableProperty]
    private int currentStepIndex;

    [ObservableProperty]
    private ObservableCollection<OnboardingStepItemViewModel> stepItems = [];

    [ObservableProperty]
    private bool familyProfileCompleted;

    [ObservableProperty]
    private bool membersCompleted;

    [ObservableProperty]
    private bool accountsCompleted;

    [ObservableProperty]
    private bool envelopesCompleted;

    [ObservableProperty]
    private bool budgetCompleted;

    [ObservableProperty]
    private bool plaidCompleted;

    [ObservableProperty]
    private bool stripeAccountsCompleted;

    [ObservableProperty]
    private bool cardsCompleted;

    [ObservableProperty]
    private bool automationCompleted;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Loading onboarding status...";

    [ObservableProperty]
    private ObservableCollection<FamilyMemberItemViewModel> familyMembers = [];

    [ObservableProperty]
    private ObservableCollection<FamilyInviteItemViewModel> familyInvites = [];

    [ObservableProperty]
    private FamilyInviteItemViewModel? selectedFamilyInvite;

    [ObservableProperty]
    private string draftInviteEmail = string.Empty;

    [ObservableProperty]
    private string draftInviteRole = InviteRoles[0];

    [ObservableProperty]
    private string draftInviteExpiresInHours = "168";

    [ObservableProperty]
    private string memberStepMessage = "Invite household members to begin family setup.";

    [ObservableProperty]
    private ObservableCollection<OnboardingAccountDraftViewModel> accountDrafts = [];

    [ObservableProperty]
    private ObservableCollection<OnboardingEnvelopeDraftViewModel> envelopeDrafts = [];

    [ObservableProperty]
    private string budgetMonth = string.Empty;

    [ObservableProperty]
    private string budgetIncome = "0";

    [ObservableProperty]
    private string familyNameDraft = string.Empty;

    [ObservableProperty]
    private string selectedCurrencyCode = DefaultCurrencyCode;

    [ObservableProperty]
    private string selectedTimeZoneId = DefaultTimeZoneId;

    public string CurrentStepTitle => Steps[Math.Clamp(CurrentStepIndex, 0, Steps.Count - 1)];

    public bool IsFirstStep => CurrentStepIndex == 0;
    public bool IsLastStep => CurrentStepIndex == Steps.Count - 1;
    public bool CanGoBack => !IsFirstStep;
    public bool CanGoNext => !IsLastStep;
    public bool IsFamilyProfileStep => CurrentStepIndex == 0;
    public bool IsFamilyMembersStep => CurrentStepIndex == 1;
    public bool IsAccountsStep => CurrentStepIndex == 2;
    public bool IsEnvelopesStep => CurrentStepIndex == 3;
    public bool IsBudgetStep => CurrentStepIndex == 4;

    public int ProgressPercent
    {
        get
        {
            var completed = CountCompletedMilestones();
            if (completed <= 0)
            {
                return 0;
            }

            return (int)Math.Round(completed * 100d / MilestoneCount, MidpointRounding.AwayFromZero);
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var profileTask = _onboardingDataService.GetProfileAsync(cancellationToken);
            var familyProfileTask = _onboardingDataService.GetFamilyProfileAsync(cancellationToken);
            var membersTask = _familyMembersDataService.GetMembersAsync(cancellationToken);
            var invitesTask = _familyMembersDataService.GetInvitesAsync(cancellationToken);

            await Task.WhenAll(profileTask, familyProfileTask, membersTask, invitesTask);

            FamilyMembers = new ObservableCollection<FamilyMemberItemViewModel>(await membersTask);
            FamilyInvites = new ObservableCollection<FamilyInviteItemViewModel>(await invitesTask);
            SelectedFamilyInvite = FamilyInvites.FirstOrDefault();

            ApplyFamilyProfile(await familyProfileTask);

            var profile = await profileTask;
            profile = await SyncMembersMilestoneFromRealStateAsync(profile, cancellationToken);
            ApplyProfile(profile);

            StatusMessage = profile.IsCompleted
                ? "Onboarding is complete."
                : "Resume setup from the next incomplete step.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to load onboarding status: {ex.Message}";
            StatusMessage = "Onboarding status unavailable.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void NextStep()
    {
        if (CurrentStepIndex < Steps.Count - 1)
        {
            CurrentStepIndex++;
        }
    }

    private void PreviousStep()
    {
        if (CurrentStepIndex > 0)
        {
            CurrentStepIndex--;
        }
    }

    private async Task MarkCurrentStepCompleteAsync(CancellationToken cancellationToken)
    {
        if (CurrentStepIndex == 0)
        {
            var saved = await SaveFamilyProfileCoreAsync(cancellationToken);
            if (saved && CurrentStepIndex == 0)
            {
                CurrentStepIndex = 1;
            }

            return;
        }

        if (CurrentStepIndex == 1)
        {
            var membersCompletion = ComputeMembersCompletedFromState();
            if (!membersCompletion)
            {
                HasError = true;
                ErrorMessage = "Add at least one pending/accepted invite or two members to complete this step.";
                return;
            }

            try
            {
                var updated = await _onboardingDataService.UpdateProfileAsync(
                    membersCompletion,
                    AccountsCompleted,
                    EnvelopesCompleted,
                    BudgetCompleted,
                    PlaidCompleted,
                    StripeAccountsCompleted,
                    CardsCompleted,
                    AutomationCompleted,
                    cancellationToken);

                ApplyProfile(updated);
                StatusMessage = "Family members step saved.";
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Unable to save family members step: {ex.Message}";
            }

            return;
        }

        var nextMembers = MembersCompleted;
        var nextAccounts = AccountsCompleted;
        var nextEnvelopes = EnvelopesCompleted;
        var nextBudget = BudgetCompleted;
        var nextPlaid = PlaidCompleted;
        var nextStripeAccounts = StripeAccountsCompleted;
        var nextCards = CardsCompleted;
        var nextAutomation = AutomationCompleted;

        switch (CurrentStepIndex)
        {
            case 2:
                nextAccounts = true;
                break;
            case 3:
                nextEnvelopes = true;
                break;
            case 4:
                nextBudget = true;
                break;
            case 5:
                nextPlaid = true;
                break;
            case 6:
                nextStripeAccounts = true;
                break;
            case 7:
                nextCards = true;
                break;
            case 8:
                nextAutomation = true;
                break;
        }

        try
        {
            var updated = await _onboardingDataService.UpdateProfileAsync(
                nextMembers,
                nextAccounts,
                nextEnvelopes,
                nextBudget,
                nextPlaid,
                nextStripeAccounts,
                nextCards,
                nextAutomation,
                cancellationToken);

            ApplyProfile(updated);
            StatusMessage = updated.IsCompleted
                ? "Onboarding marked complete."
                : "Progress saved.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to save onboarding progress: {ex.Message}";
        }
    }

    private async Task ReconcileProgressAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var reconciled = await _onboardingDataService.ReconcileProfileAsync(cancellationToken);
            reconciled = await SyncMembersMilestoneFromRealStateAsync(reconciled, cancellationToken);
            ApplyProfile(reconciled);
            StatusMessage = reconciled.IsCompleted
                ? "Onboarding reconciled and complete."
                : "Onboarding reconciled from current family data.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to reconcile onboarding progress: {ex.Message}";
        }
    }

    private async Task SaveFamilyProfileAsync(CancellationToken cancellationToken)
    {
        var saved = await SaveFamilyProfileCoreAsync(cancellationToken);
        if (saved && CurrentStepIndex == 0)
        {
            CurrentStepIndex = 1;
        }
    }

    private async Task<bool> SaveFamilyProfileCoreAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        var validationError = ValidateFamilyProfileDraft();
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            HasError = true;
            ErrorMessage = validationError;
            return false;
        }

        try
        {
            var updated = await _onboardingDataService.UpdateFamilyProfileAsync(
                FamilyNameDraft.Trim(),
                SelectedCurrencyCode.Trim().ToUpperInvariant(),
                SelectedTimeZoneId.Trim(),
                cancellationToken);

            ApplyFamilyProfile(updated);
            StatusMessage = "Family profile saved.";
            return true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to save family profile: {ex.Message}";
            return false;
        }
    }

    private async Task CreateFamilyInviteAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(DraftInviteEmail))
        {
            HasError = true;
            ErrorMessage = "Invite email is required.";
            return;
        }

        try
        {
            _ = new MailAddress(DraftInviteEmail.Trim());
        }
        catch
        {
            HasError = true;
            ErrorMessage = "Enter a valid invite email.";
            return;
        }

        if (!InviteRoles.Contains(DraftInviteRole, StringComparer.OrdinalIgnoreCase))
        {
            HasError = true;
            ErrorMessage = "Invite role is invalid.";
            return;
        }

        if (!int.TryParse(DraftInviteExpiresInHours, out var expiresInHours) || expiresInHours < 1 || expiresInHours > 720)
        {
            HasError = true;
            ErrorMessage = "Invite expiration must be between 1 and 720 hours.";
            return;
        }

        try
        {
            var created = await _familyMembersDataService.CreateInviteAsync(
                DraftInviteEmail.Trim(),
                DraftInviteRole,
                expiresInHours,
                cancellationToken);

            DraftInviteEmail = string.Empty;
            DraftInviteRole = InviteRoles[0];
            DraftInviteExpiresInHours = "168";

            await RefreshFamilyMemberStateAsync(cancellationToken);
            var updated = await SyncMembersMilestoneFromRealStateAsync(CurrentProfileSnapshot(), cancellationToken);
            ApplyProfile(updated);

            MemberStepMessage = $"Invite created for '{created.Invite.Email}'.";
            StatusMessage = MemberStepMessage;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to create invite: {ex.Message}";
        }
    }

    private async Task CancelFamilyInviteAsync(CancellationToken cancellationToken)
    {
        if (SelectedFamilyInvite is null)
        {
            HasError = true;
            ErrorMessage = "Select an invite to cancel.";
            return;
        }

        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var cancelled = await _familyMembersDataService.CancelInviteAsync(SelectedFamilyInvite.Id, cancellationToken);
            await RefreshFamilyMemberStateAsync(cancellationToken);
            var updated = await SyncMembersMilestoneFromRealStateAsync(CurrentProfileSnapshot(), cancellationToken);
            ApplyProfile(updated);

            MemberStepMessage = $"Invite for '{cancelled.Email}' cancelled.";
            StatusMessage = MemberStepMessage;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to cancel invite: {ex.Message}";
        }
    }

    private async Task RefreshFamilyMemberStateAsync(CancellationToken cancellationToken)
    {
        var members = await _familyMembersDataService.GetMembersAsync(cancellationToken);
        FamilyMembers = new ObservableCollection<FamilyMemberItemViewModel>(members);

        var invites = await _familyMembersDataService.GetInvitesAsync(cancellationToken);
        FamilyInvites = new ObservableCollection<FamilyInviteItemViewModel>(invites);

        SelectedFamilyInvite = FamilyInvites.FirstOrDefault(static invite =>
                invite.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            ?? FamilyInvites.FirstOrDefault();
    }

    private async Task<OnboardingProfileData> SyncMembersMilestoneFromRealStateAsync(
        OnboardingProfileData profile,
        CancellationToken cancellationToken)
    {
        var membersCompletedFromState = ComputeMembersCompletedFromState();
        if (profile.MembersCompleted == membersCompletedFromState)
        {
            return profile;
        }

        return await _onboardingDataService.UpdateProfileAsync(
            membersCompletedFromState,
            profile.AccountsCompleted,
            profile.EnvelopesCompleted,
            profile.BudgetCompleted,
            profile.PlaidCompleted,
            profile.StripeAccountsCompleted,
            profile.CardsCompleted,
            profile.AutomationCompleted,
            cancellationToken);
    }

    private bool ComputeMembersCompletedFromState()
    {
        if (FamilyMembers.Count >= 2)
        {
            return true;
        }

        return FamilyInvites.Any(static invite =>
            invite.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase)
            || invite.Status.Equals("Accepted", StringComparison.OrdinalIgnoreCase));
    }

    private OnboardingProfileData CurrentProfileSnapshot()
    {
        return new OnboardingProfileData(
            Guid.Empty,
            Guid.Empty,
            MembersCompleted,
            AccountsCompleted,
            EnvelopesCompleted,
            BudgetCompleted,
            PlaidCompleted,
            StripeAccountsCompleted,
            CardsCompleted,
            AutomationCompleted,
            IsCompleted: MembersCompleted
                         && AccountsCompleted
                         && EnvelopesCompleted
                         && BudgetCompleted
                         && PlaidCompleted
                         && StripeAccountsCompleted
                         && CardsCompleted
                         && AutomationCompleted,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            UpdatedAtUtc: DateTimeOffset.UtcNow,
            CompletedAtUtc: null);
    }

    private string? ValidateFamilyProfileDraft()
    {
        if (string.IsNullOrWhiteSpace(FamilyNameDraft))
        {
            return "Family name is required.";
        }

        if (string.IsNullOrWhiteSpace(SelectedCurrencyCode))
        {
            return "Currency is required.";
        }

        if (string.IsNullOrWhiteSpace(SelectedTimeZoneId))
        {
            return "Time zone is required.";
        }

        return null;
    }

    private async Task SubmitBootstrapAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        var accounts = new List<(string Name, string Type, decimal OpeningBalance)>();
        foreach (var row in AccountDrafts)
        {
            if (string.IsNullOrWhiteSpace(row.Name))
            {
                continue;
            }

            if (!decimal.TryParse(row.OpeningBalance, out var openingBalance) || openingBalance < 0m)
            {
                HasError = true;
                ErrorMessage = $"Invalid account opening balance for '{row.Name}'.";
                return;
            }

            accounts.Add((row.Name.Trim(), row.Type.Trim(), openingBalance));
        }

        var envelopes = new List<(string Name, decimal MonthlyBudget)>();
        foreach (var row in EnvelopeDrafts)
        {
            if (string.IsNullOrWhiteSpace(row.Name))
            {
                continue;
            }

            if (!decimal.TryParse(row.MonthlyBudget, out var monthlyBudget) || monthlyBudget < 0m)
            {
                HasError = true;
                ErrorMessage = $"Invalid envelope monthly budget for '{row.Name}'.";
                return;
            }

            envelopes.Add((row.Name.Trim(), monthlyBudget));
        }

        (string Month, decimal TotalIncome)? budget = null;
        if (!string.IsNullOrWhiteSpace(BudgetMonth))
        {
            if (!decimal.TryParse(BudgetIncome, out var totalIncome) || totalIncome < 0m)
            {
                HasError = true;
                ErrorMessage = "Budget income must be a non-negative number.";
                return;
            }

            budget = (BudgetMonth.Trim(), totalIncome);
        }

        try
        {
            var result = await _onboardingDataService.BootstrapAsync(accounts, envelopes, budget, cancellationToken);
            await _onboardingDataService.UpdateProfileAsync(
                MembersCompleted,
                result.AccountsCreated > 0 || AccountsCompleted,
                result.EnvelopesCreated > 0 || EnvelopesCompleted,
                result.BudgetCreated || BudgetCompleted,
                PlaidCompleted,
                StripeAccountsCompleted,
                CardsCompleted,
                AutomationCompleted,
                cancellationToken);
            StatusMessage = "Onboarding bootstrap submitted successfully.";
            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to submit onboarding bootstrap: {ex.Message}";
        }
    }

    private void AddAccountRow()
    {
        AccountDrafts.Add(new OnboardingAccountDraftViewModel());
    }

    private void RemoveAccountRow(OnboardingAccountDraftViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        AccountDrafts.Remove(row);
    }

    private void AddEnvelopeRow()
    {
        EnvelopeDrafts.Add(new OnboardingEnvelopeDraftViewModel());
    }

    private void RemoveEnvelopeRow(OnboardingEnvelopeDraftViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        EnvelopeDrafts.Remove(row);
    }

    private void Cancel()
    {
        StatusMessage = "Onboarding canceled for now. You can resume later.";
    }

    partial void OnCurrentStepIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CurrentStepTitle));
        OnPropertyChanged(nameof(IsFirstStep));
        OnPropertyChanged(nameof(IsLastStep));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(IsFamilyProfileStep));
        OnPropertyChanged(nameof(IsFamilyMembersStep));
        OnPropertyChanged(nameof(IsAccountsStep));
        OnPropertyChanged(nameof(IsEnvelopesStep));
        OnPropertyChanged(nameof(IsBudgetStep));
        RefreshStepItems();
    }

    private static int DetermineCurrentStepIndex(bool familyProfileCompleted, OnboardingProfileData profile)
    {
        if (!familyProfileCompleted)
        {
            return 0;
        }

        if (!profile.MembersCompleted)
        {
            return 1;
        }

        if (!profile.AccountsCompleted)
        {
            return 2;
        }

        if (!profile.EnvelopesCompleted)
        {
            return 3;
        }

        if (!profile.BudgetCompleted)
        {
            return 4;
        }

        if (!profile.PlaidCompleted)
        {
            return 5;
        }

        if (!profile.StripeAccountsCompleted)
        {
            return 6;
        }

        if (!profile.CardsCompleted)
        {
            return 7;
        }

        if (!profile.AutomationCompleted)
        {
            return 8;
        }

        return 9;
    }

    private void ApplyFamilyProfile(FamilyProfileData profile)
    {
        FamilyNameDraft = profile.Name;
        SelectedCurrencyCode = NormalizeCurrency(profile.CurrencyCode);
        SelectedTimeZoneId = NormalizeTimeZone(profile.TimeZoneId);
        FamilyProfileCompleted = IsFamilyProfileComplete(FamilyNameDraft, SelectedCurrencyCode, SelectedTimeZoneId);
    }

    private void ApplyProfile(OnboardingProfileData profile)
    {
        MembersCompleted = profile.MembersCompleted;
        AccountsCompleted = profile.AccountsCompleted;
        EnvelopesCompleted = profile.EnvelopesCompleted;
        BudgetCompleted = profile.BudgetCompleted;
        PlaidCompleted = profile.PlaidCompleted;
        StripeAccountsCompleted = profile.StripeAccountsCompleted;
        CardsCompleted = profile.CardsCompleted;
        AutomationCompleted = profile.AutomationCompleted;
        CurrentStepIndex = DetermineCurrentStepIndex(FamilyProfileCompleted, profile);
        RefreshStepItems();
    }

    private void RefreshStepItems()
    {
        foreach (var step in StepItems)
        {
            step.IsCompleted = IsStepCompleted(step.Index);
            step.IsCurrent = step.Index == CurrentStepIndex;
        }
    }

    private bool IsStepCompleted(int stepIndex)
    {
        return stepIndex switch
        {
            0 => FamilyProfileCompleted,
            1 => MembersCompleted,
            2 => AccountsCompleted,
            3 => EnvelopesCompleted,
            4 => BudgetCompleted,
            5 => PlaidCompleted,
            6 => StripeAccountsCompleted,
            7 => CardsCompleted,
            8 => AutomationCompleted,
            _ => CountCompletedMilestones() == MilestoneCount
        };
    }

    private int CountCompletedMilestones()
    {
        var completed = 0;
        if (FamilyProfileCompleted)
        {
            completed++;
        }

        if (MembersCompleted)
        {
            completed++;
        }

        if (AccountsCompleted)
        {
            completed++;
        }

        if (EnvelopesCompleted)
        {
            completed++;
        }

        if (BudgetCompleted)
        {
            completed++;
        }

        if (PlaidCompleted)
        {
            completed++;
        }

        if (StripeAccountsCompleted)
        {
            completed++;
        }

        if (CardsCompleted)
        {
            completed++;
        }

        if (AutomationCompleted)
        {
            completed++;
        }

        return completed;
    }

    private static bool IsFamilyProfileComplete(string? name, string? currencyCode, string? timeZoneId)
    {
        return !string.IsNullOrWhiteSpace(name)
               && !string.IsNullOrWhiteSpace(currencyCode)
               && !string.IsNullOrWhiteSpace(timeZoneId);
    }

    private string NormalizeCurrency(string? currencyCode)
    {
        if (!string.IsNullOrWhiteSpace(currencyCode))
        {
            var normalized = currencyCode.Trim().ToUpperInvariant();
            if (CurrencyOptions.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                return normalized;
            }
        }

        return DefaultCurrencyCode;
    }

    private string NormalizeTimeZone(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            var normalized = timeZoneId.Trim();
            if (TimeZoneOptions.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                return normalized;
            }

            return normalized;
        }

        return DefaultTimeZoneId;
    }

    partial void OnFamilyProfileCompletedChanged(bool value) => OnPropertyChanged(nameof(ProgressPercent));
    partial void OnMembersCompletedChanged(bool value) => OnPropertyChanged(nameof(ProgressPercent));
    partial void OnAccountsCompletedChanged(bool value) => OnPropertyChanged(nameof(ProgressPercent));
    partial void OnEnvelopesCompletedChanged(bool value) => OnPropertyChanged(nameof(ProgressPercent));
    partial void OnBudgetCompletedChanged(bool value) => OnPropertyChanged(nameof(ProgressPercent));
    partial void OnPlaidCompletedChanged(bool value) => OnPropertyChanged(nameof(ProgressPercent));
    partial void OnStripeAccountsCompletedChanged(bool value) => OnPropertyChanged(nameof(ProgressPercent));
    partial void OnCardsCompletedChanged(bool value) => OnPropertyChanged(nameof(ProgressPercent));
    partial void OnAutomationCompletedChanged(bool value) => OnPropertyChanged(nameof(ProgressPercent));
}
