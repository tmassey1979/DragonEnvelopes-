using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Services;
using System.Collections.ObjectModel;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class OnboardingWizardViewModel : ObservableObject
{
    private static readonly string[] StepTitles =
    [
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
    private const int MilestoneCount = 8;

    private readonly IOnboardingDataService _onboardingDataService;

    public OnboardingWizardViewModel(IOnboardingDataService onboardingDataService)
    {
        _onboardingDataService = onboardingDataService;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        NextStepCommand = new RelayCommand(NextStep);
        PreviousStepCommand = new RelayCommand(PreviousStep);
        MarkCurrentStepCompleteCommand = new AsyncRelayCommand(MarkCurrentStepCompleteAsync);
        ReconcileProgressCommand = new AsyncRelayCommand(ReconcileProgressAsync);
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
        RefreshStepItems();
        _ = LoadCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadCommand { get; }
    public IRelayCommand NextStepCommand { get; }
    public IRelayCommand PreviousStepCommand { get; }
    public IAsyncRelayCommand MarkCurrentStepCompleteCommand { get; }
    public IAsyncRelayCommand ReconcileProgressCommand { get; }
    public IRelayCommand AddAccountRowCommand { get; }
    public IRelayCommand<OnboardingAccountDraftViewModel?> RemoveAccountRowCommand { get; }
    public IRelayCommand AddEnvelopeRowCommand { get; }
    public IRelayCommand<OnboardingEnvelopeDraftViewModel?> RemoveEnvelopeRowCommand { get; }
    public IAsyncRelayCommand SubmitBootstrapCommand { get; }
    public IRelayCommand CancelCommand { get; }

    public IReadOnlyList<string> Steps { get; } = StepTitles;
    public IReadOnlyList<string> AccountTypeOptions { get; } = AccountTypes;

    [ObservableProperty]
    private int currentStepIndex;

    [ObservableProperty]
    private ObservableCollection<OnboardingStepItemViewModel> stepItems = [];

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
    private ObservableCollection<OnboardingAccountDraftViewModel> accountDrafts = [];

    [ObservableProperty]
    private ObservableCollection<OnboardingEnvelopeDraftViewModel> envelopeDrafts = [];

    [ObservableProperty]
    private string budgetMonth = string.Empty;

    [ObservableProperty]
    private string budgetIncome = "0";

    public string CurrentStepTitle => Steps[Math.Clamp(CurrentStepIndex, 0, Steps.Count - 1)];

    public bool IsFirstStep => CurrentStepIndex == 0;
    public bool IsLastStep => CurrentStepIndex == Steps.Count - 1;
    public bool CanGoBack => !IsFirstStep;
    public bool CanGoNext => !IsLastStep;

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
            var profile = await _onboardingDataService.GetProfileAsync(cancellationToken);
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
            case 0:
                nextMembers = true;
                break;
            case 1:
                nextAccounts = true;
                break;
            case 2:
                nextEnvelopes = true;
                break;
            case 3:
                nextBudget = true;
                break;
            case 4:
                nextPlaid = true;
                break;
            case 5:
                nextStripeAccounts = true;
                break;
            case 6:
                nextCards = true;
                break;
            case 7:
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
            if (!IsLastStep)
            {
                CurrentStepIndex++;
            }
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
        RefreshStepItems();
    }

    private static int DetermineCurrentStepIndex(OnboardingProfileData profile)
    {
        if (!profile.MembersCompleted)
        {
            return 0;
        }

        if (!profile.AccountsCompleted)
        {
            return 1;
        }

        if (!profile.EnvelopesCompleted)
        {
            return 2;
        }

        if (!profile.BudgetCompleted)
        {
            return 3;
        }

        if (!profile.PlaidCompleted)
        {
            return 4;
        }

        if (!profile.StripeAccountsCompleted)
        {
            return 5;
        }

        if (!profile.CardsCompleted)
        {
            return 6;
        }

        if (!profile.AutomationCompleted)
        {
            return 7;
        }

        return 8;
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
        CurrentStepIndex = DetermineCurrentStepIndex(profile);
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
            0 => MembersCompleted,
            1 => AccountsCompleted,
            2 => EnvelopesCompleted,
            3 => BudgetCompleted,
            4 => PlaidCompleted,
            5 => StripeAccountsCompleted,
            6 => CardsCompleted,
            7 => AutomationCompleted,
            _ => CountCompletedMilestones() == MilestoneCount
        };
    }

    private int CountCompletedMilestones()
    {
        var completed = 0;
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

    partial void OnMembersCompletedChanged(bool value) => OnPropertyChanged(nameof(ProgressPercent));
    partial void OnAccountsCompletedChanged(bool value) => OnPropertyChanged(nameof(ProgressPercent));
    partial void OnEnvelopesCompletedChanged(bool value) => OnPropertyChanged(nameof(ProgressPercent));
    partial void OnBudgetCompletedChanged(bool value) => OnPropertyChanged(nameof(ProgressPercent));
    partial void OnPlaidCompletedChanged(bool value) => OnPropertyChanged(nameof(ProgressPercent));
    partial void OnStripeAccountsCompletedChanged(bool value) => OnPropertyChanged(nameof(ProgressPercent));
    partial void OnCardsCompletedChanged(bool value) => OnPropertyChanged(nameof(ProgressPercent));
    partial void OnAutomationCompletedChanged(bool value) => OnPropertyChanged(nameof(ProgressPercent));
}
