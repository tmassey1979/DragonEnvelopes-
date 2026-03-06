using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Services;

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

    private readonly IOnboardingDataService _onboardingDataService;

    public OnboardingWizardViewModel(IOnboardingDataService onboardingDataService)
    {
        _onboardingDataService = onboardingDataService;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        NextStepCommand = new RelayCommand(NextStep);
        PreviousStepCommand = new RelayCommand(PreviousStep);
        MarkCurrentStepCompleteCommand = new AsyncRelayCommand(MarkCurrentStepCompleteAsync);
        CancelCommand = new RelayCommand(Cancel);
        _ = LoadCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadCommand { get; }
    public IRelayCommand NextStepCommand { get; }
    public IRelayCommand PreviousStepCommand { get; }
    public IAsyncRelayCommand MarkCurrentStepCompleteCommand { get; }
    public IRelayCommand CancelCommand { get; }

    public IReadOnlyList<string> Steps { get; } = StepTitles;

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
