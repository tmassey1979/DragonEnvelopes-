using System.Collections.ObjectModel;
using System.Globalization;
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
    private static readonly string[] PayFrequencies = ["Weekly", "BiWeekly", "SemiMonthly", "Monthly"];
    private static readonly string[] BudgetingStyles = ["ZeroBased", "EnvelopePriority"];
    private const int MilestoneCount = 9;

    private readonly IOnboardingDataService _onboardingDataService;
    private readonly IFamilyMembersDataService _familyMembersDataService;
    private readonly IFinancialIntegrationDataService? _financialIntegrationDataService;
    private readonly IAccountsDataService? _accountsDataService;
    private readonly IDesktopPlaidLinkService? _desktopPlaidLinkService;
    private readonly IReportsDataService? _reportsDataService;
    private readonly IEnvelopesDataService? _envelopesDataService;
    private readonly IAutomationRulesDataService? _automationRulesDataService;

    public OnboardingWizardViewModel(
        IOnboardingDataService onboardingDataService,
        IFamilyMembersDataService familyMembersDataService,
        IFinancialIntegrationDataService? financialIntegrationDataService = null,
        IAccountsDataService? accountsDataService = null,
        IDesktopPlaidLinkService? desktopPlaidLinkService = null,
        IReportsDataService? reportsDataService = null,
        IEnvelopesDataService? envelopesDataService = null,
        IAutomationRulesDataService? automationRulesDataService = null)
    {
        _onboardingDataService = onboardingDataService;
        _familyMembersDataService = familyMembersDataService;
        _financialIntegrationDataService = financialIntegrationDataService;
        _accountsDataService = accountsDataService;
        _desktopPlaidLinkService = desktopPlaidLinkService;
        _reportsDataService = reportsDataService;
        _envelopesDataService = envelopesDataService;
        _automationRulesDataService = automationRulesDataService;

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        NextStepCommand = new RelayCommand(NextStep);
        PreviousStepCommand = new RelayCommand(PreviousStep);
        MarkCurrentStepCompleteCommand = new AsyncRelayCommand(MarkCurrentStepCompleteAsync);
        ReconcileProgressCommand = new AsyncRelayCommand(ReconcileProgressAsync);
        SaveFamilyProfileCommand = new AsyncRelayCommand(SaveFamilyProfileAsync);
        SaveBudgetPreferencesCommand = new AsyncRelayCommand(SaveBudgetPreferencesAsync);
        CreatePlaidLinkTokenCommand = new AsyncRelayCommand(CreatePlaidLinkTokenAsync);
        LaunchNativePlaidLinkCommand = new AsyncRelayCommand(LaunchNativePlaidLinkAsync);
        ExchangePlaidPublicTokenCommand = new AsyncRelayCommand(ExchangePlaidPublicTokenAsync);
        LinkPlaidAccountCommand = new AsyncRelayCommand(LinkPlaidAccountAsync);
        RemovePlaidAccountLinkCommand = new AsyncRelayCommand(RemovePlaidAccountLinkAsync);
        SyncPlaidTransactionsCommand = new AsyncRelayCommand(SyncPlaidTransactionsAsync);
        RefreshStripeProvisioningCommand = new AsyncRelayCommand(RefreshStripeProvisioningAsync);
        ProvisionSelectedStripeAccountsCommand = new AsyncRelayCommand(ProvisionSelectedStripeAccountsAsync);
        RetryFailedStripeProvisioningCommand = new AsyncRelayCommand(RetryFailedStripeProvisioningAsync);
        RefreshCardProvisioningCommand = new AsyncRelayCommand(RefreshCardProvisioningAsync);
        IssueOnboardingVirtualCardCommand = new AsyncRelayCommand(IssueOnboardingVirtualCardAsync);
        IssueOnboardingPhysicalCardCommand = new AsyncRelayCommand(IssueOnboardingPhysicalCardAsync);
        RefreshOnboardingCardIssuanceCommand = new AsyncRelayCommand(RefreshOnboardingCardIssuanceAsync);
        SaveOnboardingCardControlsCommand = new AsyncRelayCommand(SaveOnboardingCardControlsAsync);
        RefreshOnboardingAutomationRulesCommand = new AsyncRelayCommand(RefreshOnboardingAutomationRulesAsync);
        CreateSelectedAutomationTemplatesCommand = new AsyncRelayCommand(CreateSelectedAutomationTemplatesAsync);
        ToggleSelectedAutomationRuleCommand = new AsyncRelayCommand(ToggleSelectedAutomationRuleAsync);
        LaunchDashboardCommand = new RelayCommand(LaunchDashboard);
        CreateInviteCommand = new AsyncRelayCommand(CreateFamilyInviteAsync);
        CancelInviteCommand = new AsyncRelayCommand(CancelFamilyInviteAsync);
        AddAccountRowCommand = new RelayCommand(AddAccountRow);
        RemoveAccountRowCommand = new RelayCommand<OnboardingAccountDraftViewModel?>(RemoveAccountRow);
        AddEnvelopeRowCommand = new RelayCommand(AddEnvelopeRow);
        RemoveEnvelopeRowCommand = new RelayCommand<OnboardingEnvelopeDraftViewModel?>(RemoveEnvelopeRow);
        GenerateEnvelopeSuggestionsCommand = new AsyncRelayCommand(GenerateEnvelopeSuggestionsAsync);
        RemoveEnvelopeSuggestionCommand = new RelayCommand(RemoveEnvelopeSuggestion);
        MergeEnvelopeSuggestionsCommand = new RelayCommand(MergeEnvelopeSuggestions);
        ApplyEnvelopeSuggestionsCommand = new AsyncRelayCommand(ApplyEnvelopeSuggestionsAsync);
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
        RefreshReviewSummary();
        _ = LoadCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadCommand { get; }
    public IRelayCommand NextStepCommand { get; }
    public IRelayCommand PreviousStepCommand { get; }
    public IAsyncRelayCommand MarkCurrentStepCompleteCommand { get; }
    public IAsyncRelayCommand ReconcileProgressCommand { get; }
    public IAsyncRelayCommand SaveFamilyProfileCommand { get; }
    public IAsyncRelayCommand SaveBudgetPreferencesCommand { get; }
    public IAsyncRelayCommand CreatePlaidLinkTokenCommand { get; }
    public IAsyncRelayCommand LaunchNativePlaidLinkCommand { get; }
    public IAsyncRelayCommand ExchangePlaidPublicTokenCommand { get; }
    public IAsyncRelayCommand LinkPlaidAccountCommand { get; }
    public IAsyncRelayCommand RemovePlaidAccountLinkCommand { get; }
    public IAsyncRelayCommand SyncPlaidTransactionsCommand { get; }
    public IAsyncRelayCommand RefreshStripeProvisioningCommand { get; }
    public IAsyncRelayCommand ProvisionSelectedStripeAccountsCommand { get; }
    public IAsyncRelayCommand RetryFailedStripeProvisioningCommand { get; }
    public IAsyncRelayCommand RefreshCardProvisioningCommand { get; }
    public IAsyncRelayCommand IssueOnboardingVirtualCardCommand { get; }
    public IAsyncRelayCommand IssueOnboardingPhysicalCardCommand { get; }
    public IAsyncRelayCommand RefreshOnboardingCardIssuanceCommand { get; }
    public IAsyncRelayCommand SaveOnboardingCardControlsCommand { get; }
    public IAsyncRelayCommand RefreshOnboardingAutomationRulesCommand { get; }
    public IAsyncRelayCommand CreateSelectedAutomationTemplatesCommand { get; }
    public IAsyncRelayCommand ToggleSelectedAutomationRuleCommand { get; }
    public IRelayCommand LaunchDashboardCommand { get; }
    public IAsyncRelayCommand CreateInviteCommand { get; }
    public IAsyncRelayCommand CancelInviteCommand { get; }
    public IRelayCommand AddAccountRowCommand { get; }
    public IRelayCommand<OnboardingAccountDraftViewModel?> RemoveAccountRowCommand { get; }
    public IRelayCommand AddEnvelopeRowCommand { get; }
    public IRelayCommand<OnboardingEnvelopeDraftViewModel?> RemoveEnvelopeRowCommand { get; }
    public IAsyncRelayCommand GenerateEnvelopeSuggestionsCommand { get; }
    public IRelayCommand RemoveEnvelopeSuggestionCommand { get; }
    public IRelayCommand MergeEnvelopeSuggestionsCommand { get; }
    public IAsyncRelayCommand ApplyEnvelopeSuggestionsCommand { get; }
    public IAsyncRelayCommand SubmitBootstrapCommand { get; }
    public IRelayCommand CancelCommand { get; }

    public event Action? LaunchDashboardRequested;

    public IReadOnlyList<string> Steps { get; } = StepTitles;
    public IReadOnlyList<string> AccountTypeOptions { get; } = AccountTypes;
    public IReadOnlyList<string> CurrencyOptions { get; } = CurrencyCodes;
    public IReadOnlyList<string> TimeZoneOptions { get; } = TimeZoneIds;
    public IReadOnlyList<string> InviteRoleOptions { get; } = InviteRoles;
    public IReadOnlyList<string> PayFrequencyOptions { get; } = PayFrequencies;
    public IReadOnlyList<string> BudgetingStyleOptions { get; } = BudgetingStyles;

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
    private ObservableCollection<ReportCategoryBreakdownRowViewModel> spendCategoryBreakdown = [];

    [ObservableProperty]
    private ObservableCollection<OnboardingEnvelopeSuggestionViewModel> envelopeSuggestions = [];

    [ObservableProperty]
    private OnboardingEnvelopeSuggestionViewModel? selectedEnvelopeSuggestion;

    [ObservableProperty]
    private string spendReviewMessage = "Generate spend-based suggestions after importing transactions.";

    [ObservableProperty]
    private ObservableCollection<string> spendSuggestionDecisionEvents = [];

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

    [ObservableProperty]
    private string selectedPayFrequency = PayFrequencies[0];

    [ObservableProperty]
    private string selectedBudgetingStyle = BudgetingStyles[0];

    [ObservableProperty]
    private string householdMonthlyIncomeDraft = string.Empty;

    [ObservableProperty]
    private string budgetPreferenceSummary = "No budget preferences saved yet.";

    [ObservableProperty]
    private string plaidClientName = "DragonEnvelopes Desktop";

    [ObservableProperty]
    private string plaidLinkToken = string.Empty;

    [ObservableProperty]
    private string plaidLinkTokenExpiresAt = "-";

    [ObservableProperty]
    private string plaidPublicToken = string.Empty;

    [ObservableProperty]
    private ObservableCollection<AccountListItemViewModel> plaidAvailableAccounts = [];

    [ObservableProperty]
    private AccountListItemViewModel? selectedPlaidAccount;

    [ObservableProperty]
    private string plaidAccountId = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PlaidAccountLinkItemViewModel> plaidAccountLinks = [];

    [ObservableProperty]
    private PlaidAccountLinkItemViewModel? selectedPlaidAccountLink;

    [ObservableProperty]
    private string plaidStepMessage = "Connect Plaid and link at least one account.";

    [ObservableProperty]
    private string plaidSyncSummary = "No Plaid transaction sync has been run.";

    [ObservableProperty]
    private ObservableCollection<OnboardingStripeProvisioningItemViewModel> stripeProvisioningItems = [];

    [ObservableProperty]
    private string stripeProvisioningMessage = "Select envelopes to provision Stripe financial accounts.";

    [ObservableProperty]
    private ObservableCollection<OnboardingStripeProvisioningItemViewModel> cardProvisioningEnvelopeOptions = [];

    [ObservableProperty]
    private OnboardingStripeProvisioningItemViewModel? selectedCardProvisioningEnvelope;

    [ObservableProperty]
    private ObservableCollection<PaymentCardItemViewModel> onboardingIssuedCards = [];

    [ObservableProperty]
    private PaymentCardItemViewModel? selectedOnboardingIssuedCard;

    [ObservableProperty]
    private string cardProvisioningMessage = "Issue envelope-linked cards after Stripe account provisioning.";

    [ObservableProperty]
    private string onboardingVirtualCardholderName = string.Empty;

    [ObservableProperty]
    private string onboardingPhysicalCardholderName = string.Empty;

    [ObservableProperty]
    private string onboardingShipmentRecipientName = string.Empty;

    [ObservableProperty]
    private string onboardingShipmentAddressLine1 = string.Empty;

    [ObservableProperty]
    private string onboardingShipmentAddressLine2 = string.Empty;

    [ObservableProperty]
    private string onboardingShipmentCity = string.Empty;

    [ObservableProperty]
    private string onboardingShipmentStateOrProvince = string.Empty;

    [ObservableProperty]
    private string onboardingShipmentPostalCode = string.Empty;

    [ObservableProperty]
    private string onboardingShipmentCountryCode = "US";

    [ObservableProperty]
    private string onboardingIssuanceSummary = "No physical issuance selected.";

    [ObservableProperty]
    private string onboardingCardControlDailyLimit = string.Empty;

    [ObservableProperty]
    private string onboardingCardControlCategories = string.Empty;

    [ObservableProperty]
    private string onboardingCardControlMerchants = string.Empty;

    [ObservableProperty]
    private ObservableCollection<OnboardingAutomationTemplateViewModel> onboardingAutomationTemplates = [];

    [ObservableProperty]
    private ObservableCollection<AutomationRuleListItemViewModel> onboardingAutomationRules = [];

    [ObservableProperty]
    private AutomationRuleListItemViewModel? selectedOnboardingAutomationRule;

    [ObservableProperty]
    private EnvelopeOptionViewModel? selectedAutomationAllocationEnvelope;

    [ObservableProperty]
    private ObservableCollection<EnvelopeOptionViewModel> automationAllocationEnvelopeOptions = [];

    [ObservableProperty]
    private string automationStepMessage = "Select starter templates and create rules.";

    [ObservableProperty]
    private ObservableCollection<string> reviewCompletedCapabilities = [];

    [ObservableProperty]
    private ObservableCollection<string> reviewOptionalIncompleteItems = [];

    [ObservableProperty]
    private string onboardingCompletionTimestampDisplay = "Not completed";

    [ObservableProperty]
    private string reviewSummaryMessage = "Complete onboarding steps to unlock full setup.";

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
    public bool IsPlaidStep => CurrentStepIndex == 5;
    public bool IsStripeStep => CurrentStepIndex == 6;
    public bool IsCardsStep => CurrentStepIndex == 7;
    public bool IsAutomationStep => CurrentStepIndex == 8;
    public bool IsReviewStep => CurrentStepIndex == 9;

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
            var budgetPreferencesTask = _onboardingDataService.GetBudgetPreferencesAsync(cancellationToken);
            var membersTask = _familyMembersDataService.GetMembersAsync(cancellationToken);
            var invitesTask = _familyMembersDataService.GetInvitesAsync(cancellationToken);

            await Task.WhenAll(profileTask, familyProfileTask, budgetPreferencesTask, membersTask, invitesTask);

            FamilyMembers = new ObservableCollection<FamilyMemberItemViewModel>(await membersTask);
            FamilyInvites = new ObservableCollection<FamilyInviteItemViewModel>(await invitesTask);
            SelectedFamilyInvite = FamilyInvites.FirstOrDefault();

            ApplyFamilyProfile(await familyProfileTask);
            ApplyBudgetPreferences(await budgetPreferencesTask);
            await LoadPlaidStepStateAsync(cancellationToken);
            await LoadStripeProvisioningStateAsync(cancellationToken);
            InitializeAutomationTemplates();
            await LoadOnboardingAutomationRulesAsync(cancellationToken);

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
                var saved = await SaveBudgetPreferencesCoreAsync(cancellationToken);
                if (!saved)
                {
                    return;
                }

                nextBudget = true;
                break;
            case 5:
                if (!ComputePlaidCompletedFromState())
                {
                    HasError = true;
                    ErrorMessage = "Complete Plaid account linking before marking this step done.";
                    return;
                }

                nextPlaid = true;
                break;
            case 6:
                if (!ComputeStripeAccountsCompletedFromState())
                {
                    HasError = true;
                    ErrorMessage = "Provision at least one Stripe financial account before completing this step.";
                    return;
                }

                nextStripeAccounts = true;
                break;
            case 7:
                if (!ComputeCardsCompletedFromState())
                {
                    HasError = true;
                    ErrorMessage = "Issue at least one card before completing this step.";
                    return;
                }

                nextCards = true;
                break;
            case 8:
                if (!ComputeAutomationCompletedFromState())
                {
                    HasError = true;
                    ErrorMessage = "Create at least one automation rule before completing this step.";
                    return;
                }

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

    private async Task SaveBudgetPreferencesAsync(CancellationToken cancellationToken)
    {
        await SaveBudgetPreferencesCoreAsync(cancellationToken);
    }

    private async Task<bool> SaveBudgetPreferencesCoreAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        var validationError = ValidateBudgetPreferencesDraft();
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            HasError = true;
            ErrorMessage = validationError;
            return false;
        }

        var householdIncome = ParseHouseholdMonthlyIncome(HouseholdMonthlyIncomeDraft);

        try
        {
            var updated = await _onboardingDataService.UpdateBudgetPreferencesAsync(
                SelectedPayFrequency,
                SelectedBudgetingStyle,
                householdIncome,
                cancellationToken);

            ApplyBudgetPreferences(updated);
            StatusMessage = "Budget preferences saved.";
            return true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to save budget preferences: {ex.Message}";
            return false;
        }
    }

    private async Task CreatePlaidLinkTokenAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (_financialIntegrationDataService is null)
        {
            HasError = true;
            ErrorMessage = "Plaid integration service is not configured for onboarding.";
            return;
        }

        try
        {
            var token = await _financialIntegrationDataService.CreatePlaidLinkTokenAsync(PlaidClientName, cancellationToken);
            PlaidLinkToken = token.LinkToken;
            PlaidLinkTokenExpiresAt = token.ExpiresAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            PlaidStepMessage = "Plaid link token generated. Launch native Plaid Link to continue.";
            StatusMessage = PlaidStepMessage;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to create Plaid link token: {ex.Message}";
        }
    }

    private async Task LaunchNativePlaidLinkAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (_desktopPlaidLinkService is null)
        {
            HasError = true;
            ErrorMessage = "Native Plaid link service is not configured for onboarding.";
            return;
        }

        if (string.IsNullOrWhiteSpace(PlaidLinkToken))
        {
            HasError = true;
            ErrorMessage = "Generate a Plaid link token before launching native Plaid Link.";
            return;
        }

        var result = await _desktopPlaidLinkService.LaunchAsync(PlaidLinkToken.Trim(), cancellationToken);
        if (result.Succeeded)
        {
            PlaidPublicToken = result.PublicToken ?? string.Empty;
            PlaidStepMessage = "Plaid Link completed. Exchange the public token next.";
            StatusMessage = PlaidStepMessage;
            return;
        }

        if (result.IsCanceled)
        {
            PlaidStepMessage = result.Message;
            StatusMessage = result.Message;
            return;
        }

        HasError = true;
        ErrorMessage = result.Message;
    }

    private async Task ExchangePlaidPublicTokenAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (_financialIntegrationDataService is null)
        {
            HasError = true;
            ErrorMessage = "Plaid integration service is not configured for onboarding.";
            return;
        }

        if (string.IsNullOrWhiteSpace(PlaidPublicToken))
        {
            HasError = true;
            ErrorMessage = "Plaid public token is required.";
            return;
        }

        try
        {
            var status = await _financialIntegrationDataService.ExchangePlaidPublicTokenAsync(
                PlaidPublicToken.Trim(),
                cancellationToken);

            PlaidStepMessage = status.PlaidConnected
                ? "Plaid public token exchanged successfully. Link account mappings below."
                : "Plaid token exchange completed, but provider did not report a connected status.";
            StatusMessage = PlaidStepMessage;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to exchange Plaid public token: {ex.Message}";
        }
    }

    private async Task LinkPlaidAccountAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (_financialIntegrationDataService is null || _accountsDataService is null)
        {
            HasError = true;
            ErrorMessage = "Plaid account linking services are not configured for onboarding.";
            return;
        }

        if (SelectedPlaidAccount is null)
        {
            HasError = true;
            ErrorMessage = "Select an internal account to map.";
            return;
        }

        if (string.IsNullOrWhiteSpace(PlaidAccountId))
        {
            HasError = true;
            ErrorMessage = "Plaid account id is required.";
            return;
        }

        try
        {
            await _financialIntegrationDataService.UpsertPlaidAccountLinkAsync(
                SelectedPlaidAccount.Id,
                PlaidAccountId.Trim(),
                cancellationToken);
            PlaidAccountId = string.Empty;

            await RefreshPlaidAccountLinksAsync(cancellationToken);
            PlaidStepMessage = "Plaid account mapping saved.";
            StatusMessage = PlaidStepMessage;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to save Plaid account mapping: {ex.Message}";
        }
    }

    private async Task RemovePlaidAccountLinkAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (_financialIntegrationDataService is null)
        {
            HasError = true;
            ErrorMessage = "Plaid integration service is not configured for onboarding.";
            return;
        }

        if (SelectedPlaidAccountLink is null)
        {
            HasError = true;
            ErrorMessage = "Select a Plaid account link to remove.";
            return;
        }

        try
        {
            await _financialIntegrationDataService.DeletePlaidAccountLinkAsync(SelectedPlaidAccountLink.Id, cancellationToken);
            await RefreshPlaidAccountLinksAsync(cancellationToken);
            PlaidStepMessage = "Plaid account link removed.";
            StatusMessage = PlaidStepMessage;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to remove Plaid account link: {ex.Message}";
        }
    }

    private async Task SyncPlaidTransactionsAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (_financialIntegrationDataService is null)
        {
            HasError = true;
            ErrorMessage = "Plaid integration service is not configured for onboarding.";
            return;
        }

        try
        {
            var summary = await _financialIntegrationDataService.SyncPlaidTransactionsAsync(cancellationToken);
            PlaidSyncSummary =
                $"Pulled {summary.PulledCount}, inserted {summary.InsertedCount}, deduped {summary.DedupedCount}, unmapped {summary.UnmappedCount}.";
            PlaidStepMessage = "Plaid transactions synced.";
            StatusMessage = PlaidStepMessage;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to sync Plaid transactions: {ex.Message}";
        }
    }

    private async Task RefreshStripeProvisioningAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            await LoadStripeProvisioningStateAsync(cancellationToken);
            StatusMessage = "Stripe provisioning status refreshed.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to refresh Stripe provisioning state: {ex.Message}";
        }
    }

    private async Task ProvisionSelectedStripeAccountsAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (_financialIntegrationDataService is null || _envelopesDataService is null)
        {
            HasError = true;
            ErrorMessage = "Stripe provisioning services are not configured for onboarding.";
            return;
        }

        var selected = StripeProvisioningItems
            .Where(static item => item.IsSelected)
            .ToArray();
        if (selected.Length == 0)
        {
            HasError = true;
            ErrorMessage = "Select at least one envelope to provision.";
            return;
        }

        var provisioned = 0;
        var failed = 0;

        foreach (var item in selected)
        {
            item.Status = "Provisioning";
            item.StatusDetail = "Request in progress...";

            try
            {
                var account = await _financialIntegrationDataService.LinkStripeEnvelopeFinancialAccountAsync(
                    item.EnvelopeId,
                    string.IsNullOrWhiteSpace(item.DisplayName) ? null : item.DisplayName.Trim(),
                    cancellationToken);

                item.Status = "Provisioned";
                item.StatusDetail = SensitiveValueMasker.MaskIdentifier(account.ProviderFinancialAccountId);
                provisioned += 1;
            }
            catch (Exception ex)
            {
                item.Status = "Failed";
                item.StatusDetail = ex.Message;
                failed += 1;
            }
        }

        StripeProvisioningMessage = $"Provisioned {provisioned} envelope account(s); failed {failed}. Retry failed rows as needed.";
        StatusMessage = StripeProvisioningMessage;
        RefreshCardProvisioningEnvelopeOptions();
        RefreshAutomationAllocationEnvelopeOptions();
        await LoadCardsForSelectedEnvelopeAsync(cancellationToken);
    }

    private Task RetryFailedStripeProvisioningAsync(CancellationToken cancellationToken)
    {
        foreach (var item in StripeProvisioningItems)
        {
            item.IsSelected = item.IsFailed;
        }

        return ProvisionSelectedStripeAccountsAsync(cancellationToken);
    }

    private async Task RefreshCardProvisioningAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            await LoadCardsForSelectedEnvelopeAsync(cancellationToken);
            StatusMessage = "Card provisioning state refreshed.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to refresh card provisioning state: {ex.Message}";
        }
    }

    private async Task IssueOnboardingVirtualCardAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (_financialIntegrationDataService is null || SelectedCardProvisioningEnvelope is null)
        {
            HasError = true;
            ErrorMessage = "Select a provisioned envelope before issuing a card.";
            return;
        }

        try
        {
            var card = await _financialIntegrationDataService.IssueVirtualCardAsync(
                SelectedCardProvisioningEnvelope.EnvelopeId,
                string.IsNullOrWhiteSpace(OnboardingVirtualCardholderName) ? null : OnboardingVirtualCardholderName.Trim(),
                cancellationToken);

            CardProvisioningMessage = $"Virtual card issued for {SelectedCardProvisioningEnvelope.EnvelopeName}.";
            StatusMessage = CardProvisioningMessage;

            await LoadCardsForSelectedEnvelopeAsync(cancellationToken, card.Id);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to issue virtual card: {ex.Message}";
        }
    }

    private async Task IssueOnboardingPhysicalCardAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (_financialIntegrationDataService is null || SelectedCardProvisioningEnvelope is null)
        {
            HasError = true;
            ErrorMessage = "Select a provisioned envelope before issuing a physical card.";
            return;
        }

        var validationError = ValidatePhysicalCardShippingDraft();
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            HasError = true;
            ErrorMessage = validationError;
            return;
        }

        try
        {
            var issuance = await _financialIntegrationDataService.IssuePhysicalCardAsync(
                SelectedCardProvisioningEnvelope.EnvelopeId,
                string.IsNullOrWhiteSpace(OnboardingPhysicalCardholderName) ? null : OnboardingPhysicalCardholderName.Trim(),
                OnboardingShipmentRecipientName.Trim(),
                OnboardingShipmentAddressLine1.Trim(),
                string.IsNullOrWhiteSpace(OnboardingShipmentAddressLine2) ? null : OnboardingShipmentAddressLine2.Trim(),
                OnboardingShipmentCity.Trim(),
                OnboardingShipmentStateOrProvince.Trim(),
                OnboardingShipmentPostalCode.Trim(),
                OnboardingShipmentCountryCode.Trim().ToUpperInvariant(),
                cancellationToken);

            OnboardingIssuanceSummary =
                $"Shipment {issuance.Shipment.Status} via {issuance.Shipment.Carrier ?? "pending carrier"}.";
            CardProvisioningMessage = $"Physical card issued for {SelectedCardProvisioningEnvelope.EnvelopeName}.";
            StatusMessage = CardProvisioningMessage;

            await LoadCardsForSelectedEnvelopeAsync(cancellationToken, issuance.Card.Id);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to issue physical card: {ex.Message}";
        }
    }

    private async Task RefreshOnboardingCardIssuanceAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (_financialIntegrationDataService is null
            || SelectedCardProvisioningEnvelope is null
            || SelectedOnboardingIssuedCard is null)
        {
            HasError = true;
            ErrorMessage = "Select an issued card to refresh issuance status.";
            return;
        }

        if (!SelectedOnboardingIssuedCard.IsPhysical)
        {
            OnboardingIssuanceSummary = "Selected card is virtual.";
            StatusMessage = OnboardingIssuanceSummary;
            return;
        }

        try
        {
            var issuance = await _financialIntegrationDataService.RefreshPhysicalCardIssuanceAsync(
                SelectedCardProvisioningEnvelope.EnvelopeId,
                SelectedOnboardingIssuedCard.Id,
                cancellationToken);
            OnboardingIssuanceSummary =
                $"Shipment {issuance.Shipment.Status} via {issuance.Shipment.Carrier ?? "pending carrier"}, tracking {issuance.Shipment.TrackingNumber ?? "n/a"}.";
            StatusMessage = "Physical card issuance status refreshed.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to refresh issuance status: {ex.Message}";
        }
    }

    private async Task SaveOnboardingCardControlsAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (_financialIntegrationDataService is null
            || SelectedCardProvisioningEnvelope is null
            || SelectedOnboardingIssuedCard is null)
        {
            HasError = true;
            ErrorMessage = "Select an issued card before saving control defaults.";
            return;
        }

        decimal? dailyLimit = null;
        if (!string.IsNullOrWhiteSpace(OnboardingCardControlDailyLimit))
        {
            if (!decimal.TryParse(
                    OnboardingCardControlDailyLimit,
                    NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                    CultureInfo.CurrentCulture,
                    out var parsedDailyLimit)
                || parsedDailyLimit < 0m)
            {
                HasError = true;
                ErrorMessage = "Daily limit must be a non-negative number.";
                return;
            }

            dailyLimit = parsedDailyLimit;
        }

        try
        {
            var updated = await _financialIntegrationDataService.UpsertCardControlsAsync(
                SelectedCardProvisioningEnvelope.EnvelopeId,
                SelectedOnboardingIssuedCard.Id,
                dailyLimit,
                ParseDelimitedValues(OnboardingCardControlCategories),
                ParseDelimitedValues(OnboardingCardControlMerchants),
                cancellationToken);

            OnboardingCardControlDailyLimit = updated.DailyLimitAmount?.ToString("0.##") ?? string.Empty;
            OnboardingCardControlCategories = string.Join(", ", updated.AllowedMerchantCategories);
            OnboardingCardControlMerchants = string.Join(", ", updated.AllowedMerchantNames);
            StatusMessage = "Card control defaults saved.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to save card control defaults: {ex.Message}";
        }
    }

    private void InitializeAutomationTemplates()
    {
        if (OnboardingAutomationTemplates.Count > 0)
        {
            RefreshAutomationAllocationEnvelopeOptions();
            return;
        }

        OnboardingAutomationTemplates = new ObservableCollection<OnboardingAutomationTemplateViewModel>(
        [
            new OnboardingAutomationTemplateViewModel(
                "Auto Categorize Groceries",
                "Categorization",
                10,
                true,
                false,
                "{\"merchantContains\":\"ALDI\"}",
                "{\"setCategory\":\"Groceries\"}",
                "Categorize grocery merchants automatically."),
            new OnboardingAutomationTemplateViewModel(
                "Auto Categorize Utilities",
                "Categorization",
                20,
                true,
                false,
                "{\"descriptionContains\":\"UTILITY\"}",
                "{\"setCategory\":\"Utilities\"}",
                "Categorize utility-related transactions."),
            new OnboardingAutomationTemplateViewModel(
                "Allocate 10 Percent to Selected Envelope",
                "Allocation",
                30,
                false,
                true,
                "{\"descriptionContains\":\"PAYROLL\",\"amountMin\":1}",
                "{\"targetEnvelopeId\":\"{ENVELOPE_ID}\",\"allocationType\":\"Percent\",\"value\":10}",
                "Allocate a percentage of payroll income automatically.")
        ]);

        RefreshAutomationAllocationEnvelopeOptions();
    }

    private async Task RefreshOnboardingAutomationRulesAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            await LoadOnboardingAutomationRulesAsync(cancellationToken);
            StatusMessage = "Automation rule list refreshed.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to refresh automation rules: {ex.Message}";
        }
    }

    private async Task CreateSelectedAutomationTemplatesAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (_automationRulesDataService is null)
        {
            HasError = true;
            ErrorMessage = "Automation rule service is not configured for onboarding.";
            return;
        }

        var selectedTemplates = OnboardingAutomationTemplates
            .Where(static template => template.IsSelected)
            .ToArray();
        if (selectedTemplates.Length == 0)
        {
            HasError = true;
            ErrorMessage = "Select at least one automation template to create.";
            return;
        }

        var created = 0;
        var skipped = 0;
        var existingNames = new HashSet<string>(
            OnboardingAutomationRules.Select(static rule => rule.Name),
            StringComparer.OrdinalIgnoreCase);

        foreach (var template in selectedTemplates)
        {
            if (existingNames.Contains(template.Name))
            {
                skipped += 1;
                continue;
            }

            var actionJson = template.ActionJsonTemplate;
            if (template.RequiresEnvelopeSelection)
            {
                if (SelectedAutomationAllocationEnvelope is null)
                {
                    HasError = true;
                    ErrorMessage = "Select an allocation envelope before creating allocation templates.";
                    return;
                }

                actionJson = actionJson.Replace(
                    "{ENVELOPE_ID}",
                    SelectedAutomationAllocationEnvelope.Id.ToString("D"),
                    StringComparison.Ordinal);
            }

            await _automationRulesDataService.CreateRuleAsync(
                template.Name,
                template.RuleType,
                template.Priority,
                isEnabled: true,
                template.ConditionsJsonTemplate,
                actionJson,
                cancellationToken);

            existingNames.Add(template.Name);
            created += 1;
        }

        await LoadOnboardingAutomationRulesAsync(cancellationToken);
        AutomationStepMessage = $"Created {created} automation rule(s); skipped {skipped} duplicate template(s).";
        StatusMessage = AutomationStepMessage;
    }

    private async Task ToggleSelectedAutomationRuleAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (_automationRulesDataService is null)
        {
            HasError = true;
            ErrorMessage = "Automation rule service is not configured for onboarding.";
            return;
        }

        if (SelectedOnboardingAutomationRule is null)
        {
            HasError = true;
            ErrorMessage = "Select an automation rule to enable or disable.";
            return;
        }

        var nextEnabled = !SelectedOnboardingAutomationRule.IsEnabled;
        try
        {
            await _automationRulesDataService.SetRuleEnabledAsync(
                SelectedOnboardingAutomationRule.Id,
                nextEnabled,
                cancellationToken);
            await LoadOnboardingAutomationRulesAsync(cancellationToken);

            AutomationStepMessage = nextEnabled
                ? "Automation rule enabled."
                : "Automation rule disabled.";
            StatusMessage = AutomationStepMessage;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to update automation rule state: {ex.Message}";
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

    private async Task LoadPlaidStepStateAsync(CancellationToken cancellationToken)
    {
        if (_financialIntegrationDataService is null || _accountsDataService is null)
        {
            PlaidStepMessage = "Plaid onboarding step is unavailable in this desktop configuration.";
            PlaidAvailableAccounts = [];
            PlaidAccountLinks = [];
            SelectedPlaidAccount = null;
            SelectedPlaidAccountLink = null;
            return;
        }

        var accountsTask = _accountsDataService.GetAccountsAsync(cancellationToken);
        var linksTask = _financialIntegrationDataService.ListPlaidAccountLinksAsync(cancellationToken);
        await Task.WhenAll(accountsTask, linksTask);

        PlaidAvailableAccounts = new ObservableCollection<AccountListItemViewModel>(
            (await accountsTask)
            .OrderBy(static account => account.Name, StringComparer.OrdinalIgnoreCase));

        if (SelectedPlaidAccount is null || PlaidAvailableAccounts.All(account => account.Id != SelectedPlaidAccount.Id))
        {
            SelectedPlaidAccount = PlaidAvailableAccounts.FirstOrDefault();
        }

        var linkResponses = await linksTask;
        var accountNames = PlaidAvailableAccounts.ToDictionary(static account => account.Id, static account => account.Name);
        PlaidAccountLinks = new ObservableCollection<PlaidAccountLinkItemViewModel>(
            linkResponses
                .OrderByDescending(static link => link.UpdatedAtUtc)
                .Select(link => new PlaidAccountLinkItemViewModel(
                    link.Id,
                    link.AccountId,
                    accountNames.TryGetValue(link.AccountId, out var accountName) ? accountName : link.AccountId.ToString("D"),
                    SensitiveValueMasker.MaskIdentifier(link.PlaidAccountId),
                    link.UpdatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"))));

        if (SelectedPlaidAccountLink is null || PlaidAccountLinks.All(link => link.Id != SelectedPlaidAccountLink.Id))
        {
            SelectedPlaidAccountLink = PlaidAccountLinks.FirstOrDefault();
        }

        PlaidStepMessage = PlaidAccountLinks.Count == 0
            ? "No Plaid account mappings yet. Complete token exchange and add at least one mapping."
            : $"Plaid mappings ready: {PlaidAccountLinks.Count} linked account(s).";
    }

    private async Task LoadStripeProvisioningStateAsync(CancellationToken cancellationToken)
    {
        if (_financialIntegrationDataService is null || _envelopesDataService is null)
        {
            StripeProvisioningItems = [];
            StripeProvisioningMessage = "Stripe provisioning step is unavailable in this desktop configuration.";
            return;
        }

        var envelopes = await _envelopesDataService.GetEnvelopesAsync(cancellationToken);
        var eligible = envelopes
            .Where(static envelope => !envelope.IsArchived)
            .OrderBy(static envelope => envelope.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var items = new List<OnboardingStripeProvisioningItemViewModel>(eligible.Length);
        foreach (var envelope in eligible)
        {
            var account = await _financialIntegrationDataService.GetEnvelopeFinancialAccountAsync(envelope.Id, cancellationToken);
            if (account is null)
            {
                items.Add(new OnboardingStripeProvisioningItemViewModel(
                    envelope.Id,
                    envelope.Name,
                    displayName: envelope.Name,
                    isSelected: false,
                    status: "NotProvisioned",
                    statusDetail: "No Stripe financial account linked."));
                continue;
            }

            items.Add(new OnboardingStripeProvisioningItemViewModel(
                envelope.Id,
                envelope.Name,
                displayName: envelope.Name,
                isSelected: false,
                status: "Provisioned",
                statusDetail: SensitiveValueMasker.MaskIdentifier(account.ProviderFinancialAccountId)));
        }

        StripeProvisioningItems = new ObservableCollection<OnboardingStripeProvisioningItemViewModel>(items);
        StripeProvisioningMessage = items.Count == 0
            ? "No eligible envelopes available for Stripe provisioning."
            : $"Loaded {items.Count} envelope(s) for Stripe provisioning.";

        RefreshCardProvisioningEnvelopeOptions();
        RefreshAutomationAllocationEnvelopeOptions();
        await LoadCardsForSelectedEnvelopeAsync(cancellationToken);
    }

    private void RefreshCardProvisioningEnvelopeOptions()
    {
        var previouslySelectedId = SelectedCardProvisioningEnvelope?.EnvelopeId;
        CardProvisioningEnvelopeOptions = new ObservableCollection<OnboardingStripeProvisioningItemViewModel>(
            StripeProvisioningItems
                .Where(static item => item.IsProvisioned)
                .OrderBy(static item => item.EnvelopeName, StringComparer.OrdinalIgnoreCase));

        SelectedCardProvisioningEnvelope = previouslySelectedId.HasValue
            ? CardProvisioningEnvelopeOptions.FirstOrDefault(item => item.EnvelopeId == previouslySelectedId.Value)
            : CardProvisioningEnvelopeOptions.FirstOrDefault();

        if (SelectedCardProvisioningEnvelope is null)
        {
            OnboardingIssuedCards = [];
            SelectedOnboardingIssuedCard = null;
            CardProvisioningMessage = "Provision an envelope account before issuing cards.";
            OnboardingIssuanceSummary = "No physical issuance selected.";
        }
    }

    private async Task LoadCardsForSelectedEnvelopeAsync(
        CancellationToken cancellationToken,
        Guid? selectedCardId = null)
    {
        if (_financialIntegrationDataService is null || SelectedCardProvisioningEnvelope is null)
        {
            OnboardingIssuedCards = [];
            SelectedOnboardingIssuedCard = null;
            OnboardingIssuanceSummary = "No physical issuance selected.";
            return;
        }

        var cards = await _financialIntegrationDataService.ListEnvelopeCardsAsync(
            SelectedCardProvisioningEnvelope.EnvelopeId,
            cancellationToken);
        OnboardingIssuedCards = new ObservableCollection<PaymentCardItemViewModel>(
            cards
                .OrderByDescending(static card => card.CreatedAtUtc)
                .Select(MapOnboardingCard));

        SelectedOnboardingIssuedCard = selectedCardId.HasValue
            ? OnboardingIssuedCards.FirstOrDefault(card => card.Id == selectedCardId.Value)
            : OnboardingIssuedCards.FirstOrDefault();

        if (SelectedOnboardingIssuedCard is null)
        {
            OnboardingIssuanceSummary = "No cards issued for the selected envelope yet.";
            OnboardingCardControlDailyLimit = string.Empty;
            OnboardingCardControlCategories = string.Empty;
            OnboardingCardControlMerchants = string.Empty;
            return;
        }

        if (SelectedOnboardingIssuedCard.IsPhysical)
        {
            var issuance = await _financialIntegrationDataService.GetPhysicalCardIssuanceAsync(
                SelectedCardProvisioningEnvelope.EnvelopeId,
                SelectedOnboardingIssuedCard.Id,
                cancellationToken);
            OnboardingIssuanceSummary = issuance is null
                ? "No issuance status was returned for this physical card."
                : $"Shipment {issuance.Shipment.Status} via {issuance.Shipment.Carrier ?? "pending carrier"}.";
        }
        else
        {
            OnboardingIssuanceSummary = "Selected card is virtual.";
        }

        var controls = await _financialIntegrationDataService.GetCardControlsAsync(
            SelectedCardProvisioningEnvelope.EnvelopeId,
            SelectedOnboardingIssuedCard.Id,
            cancellationToken);
        if (controls is null)
        {
            OnboardingCardControlDailyLimit = string.Empty;
            OnboardingCardControlCategories = string.Empty;
            OnboardingCardControlMerchants = string.Empty;
            return;
        }

        OnboardingCardControlDailyLimit = controls.DailyLimitAmount?.ToString("0.##") ?? string.Empty;
        OnboardingCardControlCategories = string.Join(", ", controls.AllowedMerchantCategories);
        OnboardingCardControlMerchants = string.Join(", ", controls.AllowedMerchantNames);
    }

    private async Task LoadOnboardingAutomationRulesAsync(CancellationToken cancellationToken)
    {
        if (_automationRulesDataService is null)
        {
            OnboardingAutomationRules = [];
            AutomationStepMessage = "Automation onboarding step is unavailable in this desktop configuration.";
            return;
        }

        var rules = await _automationRulesDataService.GetRulesAsync(typeFilter: null, enabledFilter: null, cancellationToken);
        OnboardingAutomationRules = new ObservableCollection<AutomationRuleListItemViewModel>(
            rules.OrderBy(static rule => rule.Priority));

        SelectedOnboardingAutomationRule = OnboardingAutomationRules.FirstOrDefault();
        AutomationStepMessage = OnboardingAutomationRules.Count == 0
            ? "No automation rules created yet."
            : $"Loaded {OnboardingAutomationRules.Count} automation rule(s).";
    }

    private void RefreshAutomationAllocationEnvelopeOptions()
    {
        var options = StripeProvisioningItems
            .Where(static item => item.IsProvisioned)
            .OrderBy(static item => item.EnvelopeName, StringComparer.OrdinalIgnoreCase)
            .Select(static item => new EnvelopeOptionViewModel(item.EnvelopeId, item.EnvelopeName))
            .ToArray();

        var previousSelection = SelectedAutomationAllocationEnvelope?.Id;
        AutomationAllocationEnvelopeOptions = new ObservableCollection<EnvelopeOptionViewModel>(options);
        SelectedAutomationAllocationEnvelope = previousSelection.HasValue
            ? AutomationAllocationEnvelopeOptions.FirstOrDefault(option => option.Id == previousSelection.Value)
            : AutomationAllocationEnvelopeOptions.FirstOrDefault();
    }

    private async Task RefreshPlaidAccountLinksAsync(CancellationToken cancellationToken)
    {
        if (_financialIntegrationDataService is null)
        {
            return;
        }

        var links = await _financialIntegrationDataService.ListPlaidAccountLinksAsync(cancellationToken);
        var accountNames = PlaidAvailableAccounts.ToDictionary(static account => account.Id, static account => account.Name);
        PlaidAccountLinks = new ObservableCollection<PlaidAccountLinkItemViewModel>(
            links
                .OrderByDescending(static link => link.UpdatedAtUtc)
                .Select(link => new PlaidAccountLinkItemViewModel(
                    link.Id,
                    link.AccountId,
                    accountNames.TryGetValue(link.AccountId, out var name) ? name : link.AccountId.ToString("D"),
                    SensitiveValueMasker.MaskIdentifier(link.PlaidAccountId),
                    link.UpdatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"))));

        SelectedPlaidAccountLink = PlaidAccountLinks.FirstOrDefault();
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

    private bool ComputePlaidCompletedFromState()
    {
        return PlaidAccountLinks.Count > 0;
    }

    private bool ComputeStripeAccountsCompletedFromState()
    {
        return StripeProvisioningItems.Any(static item => item.IsProvisioned);
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

    private string? ValidateBudgetPreferencesDraft()
    {
        if (!PayFrequencyOptions.Contains(SelectedPayFrequency, StringComparer.OrdinalIgnoreCase))
        {
            return "Select a valid pay frequency.";
        }

        if (!BudgetingStyleOptions.Contains(SelectedBudgetingStyle, StringComparer.OrdinalIgnoreCase))
        {
            return "Select a valid budgeting style.";
        }

        if (string.IsNullOrWhiteSpace(HouseholdMonthlyIncomeDraft))
        {
            return null;
        }

        var parsed = ParseHouseholdMonthlyIncome(HouseholdMonthlyIncomeDraft);
        if (!parsed.HasValue)
        {
            return "Household monthly income must be a non-negative number when provided.";
        }

        return null;
    }

    private static decimal? ParseHouseholdMonthlyIncome(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (decimal.TryParse(
                normalized,
                NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                CultureInfo.CurrentCulture,
                out var currentCultureValue)
            && currentCultureValue >= 0m)
        {
            return currentCultureValue;
        }

        if (decimal.TryParse(
                normalized,
                NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                CultureInfo.InvariantCulture,
                out var invariantCultureValue)
            && invariantCultureValue >= 0m)
        {
            return invariantCultureValue;
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

    private async Task GenerateEnvelopeSuggestionsAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (_reportsDataService is null)
        {
            HasError = true;
            ErrorMessage = "Reports service is not configured for spend review suggestions.";
            return;
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            var from = now.AddMonths(-3);
            var workspace = await _reportsDataService.GetWorkspaceAsync(
                now.ToString("yyyy-MM"),
                from,
                now,
                includeArchived: false,
                cancellationToken);

            SpendCategoryBreakdown = new ObservableCollection<ReportCategoryBreakdownRowViewModel>(
                workspace.CategoryBreakdown
                    .OrderByDescending(static category => category.TotalSpend)
                    .Select(static category => new ReportCategoryBreakdownRowViewModel(
                        category.Category,
                        category.TotalSpend.ToString("$#,##0.00"))));

            EnvelopeSuggestions = new ObservableCollection<OnboardingEnvelopeSuggestionViewModel>(
                workspace.CategoryBreakdown
                    .Where(static category => !string.IsNullOrWhiteSpace(category.Category))
                    .OrderByDescending(static category => category.TotalSpend)
                    .Select(category => new OnboardingEnvelopeSuggestionViewModel(
                        category.Category.Trim(),
                        BuildDefaultEnvelopeName(category.Category),
                        Math.Abs(category.TotalSpend).ToString("0.##"))));

            SelectedEnvelopeSuggestion = EnvelopeSuggestions.FirstOrDefault();
            SpendReviewMessage = EnvelopeSuggestions.Count == 0
                ? "No category spend data found for suggestion generation."
                : $"Generated {EnvelopeSuggestions.Count} envelope suggestions from recent category spend.";
            AppendSuggestionDecisionEvent(SpendReviewMessage);
            StatusMessage = SpendReviewMessage;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to generate spend suggestions: {ex.Message}";
        }
    }

    private void RemoveEnvelopeSuggestion()
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (SelectedEnvelopeSuggestion is null)
        {
            HasError = true;
            ErrorMessage = "Select an envelope suggestion to remove.";
            return;
        }

        var removedName = SelectedEnvelopeSuggestion.EnvelopeName;
        EnvelopeSuggestions.Remove(SelectedEnvelopeSuggestion);
        SelectedEnvelopeSuggestion = EnvelopeSuggestions.FirstOrDefault();
        SpendReviewMessage = $"Removed suggestion '{removedName}'.";
        AppendSuggestionDecisionEvent(SpendReviewMessage);
        StatusMessage = SpendReviewMessage;
    }

    private void MergeEnvelopeSuggestions()
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (EnvelopeSuggestions.Count < 2)
        {
            HasError = true;
            ErrorMessage = "At least two suggestions are required to merge.";
            return;
        }

        var normalizedGroups = EnvelopeSuggestions
            .GroupBy(
                static suggestion => suggestion.EnvelopeName.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .Where(static group => group.Count() > 1)
            .ToArray();

        if (normalizedGroups.Length == 0)
        {
            HasError = true;
            ErrorMessage = "No duplicate envelope names were found to merge.";
            return;
        }

        foreach (var group in normalizedGroups)
        {
            var items = group.ToArray();
            var primary = items[0];
            var mergedBudget = items.Sum(static item => item.GetMonthlyBudgetOrZero());
            var mergedCategory = string.Join(
                " + ",
                items
                    .Select(static item => item.Category)
                    .Where(static category => !string.IsNullOrWhiteSpace(category))
                    .Distinct(StringComparer.OrdinalIgnoreCase));

            primary.MonthlyBudget = mergedBudget.ToString("0.##");
            primary.Category = string.IsNullOrWhiteSpace(mergedCategory)
                ? primary.Category
                : mergedCategory;

            foreach (var duplicate in items.Skip(1))
            {
                EnvelopeSuggestions.Remove(duplicate);
            }
        }

        SelectedEnvelopeSuggestion = EnvelopeSuggestions.FirstOrDefault();
        SpendReviewMessage = "Merged duplicate envelope-name suggestions.";
        AppendSuggestionDecisionEvent(SpendReviewMessage);
        StatusMessage = SpendReviewMessage;
    }

    private async Task ApplyEnvelopeSuggestionsAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (_envelopesDataService is null)
        {
            HasError = true;
            ErrorMessage = "Envelope service is not configured for applying suggestions.";
            return;
        }

        if (EnvelopeSuggestions.Count == 0)
        {
            HasError = true;
            ErrorMessage = "No suggestions are available to apply.";
            return;
        }

        var appliedCount = 0;
        var skippedCount = 0;
        var existing = await _envelopesDataService.GetEnvelopesAsync(cancellationToken);
        var existingNames = new HashSet<string>(
            existing.Select(static envelope => envelope.Name.Trim()),
            StringComparer.OrdinalIgnoreCase);

        foreach (var suggestion in EnvelopeSuggestions.ToArray())
        {
            var name = suggestion.EnvelopeName.Trim();
            var parsedBudget = suggestion.GetMonthlyBudgetOrZero();
            if (string.IsNullOrWhiteSpace(name))
            {
                skippedCount += 1;
                continue;
            }

            if (existingNames.Contains(name))
            {
                skippedCount += 1;
                continue;
            }

            await _envelopesDataService.CreateEnvelopeAsync(name, parsedBudget, cancellationToken);
            existingNames.Add(name);
            appliedCount += 1;

            if (!EnvelopeDrafts.Any(draft => draft.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                EnvelopeDrafts.Add(new OnboardingEnvelopeDraftViewModel
                {
                    Name = name,
                    MonthlyBudget = parsedBudget.ToString("0.##")
                });
            }
        }

        SpendReviewMessage = $"Applied {appliedCount} suggestion(s); skipped {skippedCount}.";
        AppendSuggestionDecisionEvent(SpendReviewMessage);
        StatusMessage = SpendReviewMessage;
    }

    private void Cancel()
    {
        StatusMessage = "Onboarding canceled for now. You can resume later.";
    }

    partial void OnSelectedCardProvisioningEnvelopeChanged(OnboardingStripeProvisioningItemViewModel? value)
    {
        _ = RefreshCardProvisioningCommand.ExecuteAsync(null);
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
        OnPropertyChanged(nameof(IsPlaidStep));
        OnPropertyChanged(nameof(IsStripeStep));
        OnPropertyChanged(nameof(IsCardsStep));
        OnPropertyChanged(nameof(IsAutomationStep));
        OnPropertyChanged(nameof(IsReviewStep));
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
        RefreshReviewSummary();
    }

    private void ApplyBudgetPreferences(FamilyBudgetPreferencesData preferences)
    {
        SelectedPayFrequency = NormalizePayFrequency(preferences.PayFrequency);
        SelectedBudgetingStyle = NormalizeBudgetingStyle(preferences.BudgetingStyle);
        HouseholdMonthlyIncomeDraft = preferences.HouseholdMonthlyIncome.HasValue
            ? preferences.HouseholdMonthlyIncome.Value.ToString("0.##", CultureInfo.CurrentCulture)
            : string.Empty;
        BudgetPreferenceSummary = BuildBudgetPreferenceSummary(preferences);
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
        OnboardingCompletionTimestampDisplay = profile.CompletedAtUtc.HasValue
            ? profile.CompletedAtUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
            : "Not completed";
        CurrentStepIndex = DetermineCurrentStepIndex(FamilyProfileCompleted, profile);
        RefreshStepItems();
        RefreshReviewSummary();
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

    private string NormalizePayFrequency(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var normalized = value.Trim();
            var matched = PayFrequencyOptions.FirstOrDefault(option =>
                option.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(matched))
            {
                return matched;
            }
        }

        return PayFrequencies[0];
    }

    private string NormalizeBudgetingStyle(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var normalized = value.Trim();
            var matched = BudgetingStyleOptions.FirstOrDefault(option =>
                option.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(matched))
            {
                return matched;
            }
        }

        return BudgetingStyles[0];
    }

    private static string BuildBudgetPreferenceSummary(FamilyBudgetPreferencesData preferences)
    {
        if (string.IsNullOrWhiteSpace(preferences.PayFrequency) || string.IsNullOrWhiteSpace(preferences.BudgetingStyle))
        {
            return "No budget preferences saved yet.";
        }

        var income = preferences.HouseholdMonthlyIncome.HasValue
            ? preferences.HouseholdMonthlyIncome.Value.ToString("C2", CultureInfo.CurrentCulture)
            : "Not provided";

        return $"Pay {preferences.PayFrequency} | Style {preferences.BudgetingStyle} | Income {income}";
    }

    private void AppendSuggestionDecisionEvent(string detail)
    {
        var timestamped = $"{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - {detail}";
        SpendSuggestionDecisionEvents.Insert(0, timestamped);

        while (SpendSuggestionDecisionEvents.Count > 50)
        {
            SpendSuggestionDecisionEvents.RemoveAt(SpendSuggestionDecisionEvents.Count - 1);
        }
    }

    private static string BuildDefaultEnvelopeName(string category)
    {
        var normalized = category.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "Uncategorized";
        }

        return string.Join(
            ' ',
            normalized
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(static segment => segment.Length == 1
                    ? segment.ToUpperInvariant()
                    : char.ToUpperInvariant(segment[0]) + segment[1..].ToLowerInvariant()));
    }

    private string? ValidatePhysicalCardShippingDraft()
    {
        if (string.IsNullOrWhiteSpace(OnboardingShipmentRecipientName))
        {
            return "Physical card recipient name is required.";
        }

        if (string.IsNullOrWhiteSpace(OnboardingShipmentAddressLine1))
        {
            return "Physical card address line 1 is required.";
        }

        if (string.IsNullOrWhiteSpace(OnboardingShipmentCity))
        {
            return "Physical card city is required.";
        }

        if (string.IsNullOrWhiteSpace(OnboardingShipmentStateOrProvince))
        {
            return "Physical card state/province is required.";
        }

        if (string.IsNullOrWhiteSpace(OnboardingShipmentPostalCode))
        {
            return "Physical card postal code is required.";
        }

        if (string.IsNullOrWhiteSpace(OnboardingShipmentCountryCode) || OnboardingShipmentCountryCode.Trim().Length != 2)
        {
            return "Physical card country code must be a 2-letter value.";
        }

        return null;
    }

    private static IReadOnlyList<string> ParseDelimitedValues(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        return input
            .Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static PaymentCardItemViewModel MapOnboardingCard(DragonEnvelopes.Contracts.Financial.EnvelopePaymentCardResponse card)
    {
        return new PaymentCardItemViewModel(
            card.Id,
            card.EnvelopeId,
            card.Provider,
            SensitiveValueMasker.MaskIdentifier(card.ProviderCardId),
            card.Type,
            card.Status,
            string.IsNullOrWhiteSpace(card.Brand) ? "-" : card.Brand,
            string.IsNullOrWhiteSpace(card.Last4) ? "-" : card.Last4,
            card.CreatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
            card.UpdatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
    }

    private bool ComputeCardsCompletedFromState()
    {
        return OnboardingIssuedCards.Count > 0;
    }

    private bool ComputeAutomationCompletedFromState()
    {
        return OnboardingAutomationRules.Count > 0;
    }

    private void RefreshReviewSummary()
    {
        var completed = new List<string>();
        if (FamilyProfileCompleted)
        {
            completed.Add("Family profile defaults configured");
        }

        if (MembersCompleted)
        {
            completed.Add("Household members/invites configured");
        }

        if (AccountsCompleted)
        {
            completed.Add("Accounts setup complete");
        }

        if (EnvelopesCompleted)
        {
            completed.Add("Envelopes created");
        }

        if (BudgetCompleted)
        {
            completed.Add("Budget preferences/baseline saved");
        }

        if (PlaidCompleted)
        {
            completed.Add("Plaid connected and mapped");
        }

        if (StripeAccountsCompleted)
        {
            completed.Add("Stripe financial accounts provisioned");
        }

        if (CardsCompleted)
        {
            completed.Add("Cards issued and configured");
        }

        if (AutomationCompleted)
        {
            completed.Add("Automation rules configured");
        }

        ReviewCompletedCapabilities = new ObservableCollection<string>(completed);

        var optionalIncomplete = new List<string>();
        if (!PlaidCompleted)
        {
            optionalIncomplete.Add("Plaid bank connection");
        }

        if (!StripeAccountsCompleted)
        {
            optionalIncomplete.Add("Stripe financial account provisioning");
        }

        if (!CardsCompleted)
        {
            optionalIncomplete.Add("Card issuance workflow");
        }

        if (!AutomationCompleted)
        {
            optionalIncomplete.Add("Automation starter rules");
        }

        ReviewOptionalIncompleteItems = new ObservableCollection<string>(optionalIncomplete);
        ReviewSummaryMessage = optionalIncomplete.Count == 0
            ? "All onboarding capabilities are configured."
            : $"{optionalIncomplete.Count} optional capability areas are still incomplete.";
    }

    private void LaunchDashboard()
    {
        StatusMessage = "Launching dashboard...";
        LaunchDashboardRequested?.Invoke();
    }

    partial void OnFamilyProfileCompletedChanged(bool value)
    {
        OnPropertyChanged(nameof(ProgressPercent));
        RefreshReviewSummary();
    }

    partial void OnMembersCompletedChanged(bool value)
    {
        OnPropertyChanged(nameof(ProgressPercent));
        RefreshReviewSummary();
    }

    partial void OnAccountsCompletedChanged(bool value)
    {
        OnPropertyChanged(nameof(ProgressPercent));
        RefreshReviewSummary();
    }

    partial void OnEnvelopesCompletedChanged(bool value)
    {
        OnPropertyChanged(nameof(ProgressPercent));
        RefreshReviewSummary();
    }

    partial void OnBudgetCompletedChanged(bool value)
    {
        OnPropertyChanged(nameof(ProgressPercent));
        RefreshReviewSummary();
    }

    partial void OnPlaidCompletedChanged(bool value)
    {
        OnPropertyChanged(nameof(ProgressPercent));
        RefreshReviewSummary();
    }

    partial void OnStripeAccountsCompletedChanged(bool value)
    {
        OnPropertyChanged(nameof(ProgressPercent));
        RefreshReviewSummary();
    }

    partial void OnCardsCompletedChanged(bool value)
    {
        OnPropertyChanged(nameof(ProgressPercent));
        RefreshReviewSummary();
    }

    partial void OnAutomationCompletedChanged(bool value)
    {
        OnPropertyChanged(nameof(ProgressPercent));
        RefreshReviewSummary();
    }
}
