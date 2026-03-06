using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Services;
using System.Collections.ObjectModel;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class OnboardingWizardViewModel : ObservableObject
{
    private static readonly string[] StepTitles =
    [
        "Accounts",
        "Envelopes",
        "Starter Budget",
        "Review"
    ];
    private static readonly string[] AccountTypes = ["Checking", "Savings", "Cash", "Credit"];

    private readonly IOnboardingDataService _onboardingDataService;

    public OnboardingWizardViewModel(IOnboardingDataService onboardingDataService)
    {
        _onboardingDataService = onboardingDataService;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        NextStepCommand = new RelayCommand(NextStep);
        PreviousStepCommand = new RelayCommand(PreviousStep);
        MarkCurrentStepCompleteCommand = new AsyncRelayCommand(MarkCurrentStepCompleteAsync);
        AddAccountRowCommand = new RelayCommand(AddAccountRow);
        RemoveAccountRowCommand = new RelayCommand<OnboardingAccountDraftViewModel?>(RemoveAccountRow);
        AddEnvelopeRowCommand = new RelayCommand(AddEnvelopeRow);
        RemoveEnvelopeRowCommand = new RelayCommand<OnboardingEnvelopeDraftViewModel?>(RemoveEnvelopeRow);
        SubmitBootstrapCommand = new AsyncRelayCommand(SubmitBootstrapAsync);
        CancelCommand = new RelayCommand(Cancel);
        AddAccountRow();
        AddEnvelopeRow();
        BudgetMonth = DateTime.UtcNow.ToString("yyyy-MM");
        BudgetIncome = "0";
        _ = LoadCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadCommand { get; }
    public IRelayCommand NextStepCommand { get; }
    public IRelayCommand PreviousStepCommand { get; }
    public IAsyncRelayCommand MarkCurrentStepCompleteCommand { get; }
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
    private bool accountsCompleted;

    [ObservableProperty]
    private bool envelopesCompleted;

    [ObservableProperty]
    private bool budgetCompleted;

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
            var completed = 0;
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

            return completed * 33;
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
            AccountsCompleted = profile.AccountsCompleted;
            EnvelopesCompleted = profile.EnvelopesCompleted;
            BudgetCompleted = profile.BudgetCompleted;

            CurrentStepIndex = !profile.AccountsCompleted
                ? 0
                : !profile.EnvelopesCompleted
                    ? 1
                    : !profile.BudgetCompleted
                        ? 2
                        : 3;

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
        var nextAccounts = AccountsCompleted || CurrentStepIndex >= 0;
        var nextEnvelopes = EnvelopesCompleted || CurrentStepIndex >= 1;
        var nextBudget = BudgetCompleted || CurrentStepIndex >= 2;

        try
        {
            var updated = await _onboardingDataService.UpdateProfileAsync(
                nextAccounts,
                nextEnvelopes,
                nextBudget,
                cancellationToken);

            AccountsCompleted = updated.AccountsCompleted;
            EnvelopesCompleted = updated.EnvelopesCompleted;
            BudgetCompleted = updated.BudgetCompleted;
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
                result.AccountsCreated > 0 || AccountsCompleted,
                result.EnvelopesCreated > 0 || EnvelopesCompleted,
                result.BudgetCreated || BudgetCompleted,
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
    }

    partial void OnAccountsCompletedChanged(bool value) => OnPropertyChanged(nameof(ProgressPercent));
    partial void OnEnvelopesCompletedChanged(bool value) => OnPropertyChanged(nameof(ProgressPercent));
    partial void OnBudgetCompletedChanged(bool value) => OnPropertyChanged(nameof(ProgressPercent));
}
