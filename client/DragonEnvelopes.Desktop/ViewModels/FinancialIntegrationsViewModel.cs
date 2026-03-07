using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Contracts.Financial;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class FinancialIntegrationsViewModel : ObservableObject
{
    private readonly IFinancialIntegrationDataService _financialIntegrationDataService;
    private readonly IAccountsDataService _accountsDataService;
    private readonly IEnvelopesDataService _envelopesDataService;
    private readonly IDesktopPlaidLinkService _desktopPlaidLinkService;
    private bool _isApplyingEnvelopeSelection;
    private bool _isApplyingCardSelection;

    public FinancialIntegrationsViewModel(
        IFinancialIntegrationDataService financialIntegrationDataService,
        IAccountsDataService accountsDataService,
        IEnvelopesDataService envelopesDataService,
        IDesktopPlaidLinkService desktopPlaidLinkService)
    {
        _financialIntegrationDataService = financialIntegrationDataService;
        _accountsDataService = accountsDataService;
        _envelopesDataService = envelopesDataService;
        _desktopPlaidLinkService = desktopPlaidLinkService;

        PlaidClientName = "DragonEnvelopes Desktop";
        ShipmentCountryCode = "US";
        PlaidSyncSummary = "Plaid sync has not been run.";
        PlaidBalanceRefreshSummary = "Plaid balance refresh has not been run.";
        PlaidReconciliationGeneratedAt = "-";
        StripeSetupIntentId = "-";
        StripeSetupCustomerId = "-";
        StripeClientSecret = "-";
        FinancialStatusUpdatedAt = "-";
        PlaidItemIdentifier = "-";
        StripeCustomerIdentifier = "-";
        SelectedEnvelopeSummary = "Select an envelope to manage cards.";
        EnvelopeFinancialAccountSummary = "No linked financial account.";
        IssuanceSummary = "No physical card issuance selected.";
        CardControlSummary = "No spending controls loaded.";
        CardSpendEvaluationResult = "No card spend evaluation has been run.";
        ProviderHealthStatusSummary = "Provider activity has not been loaded.";
        ProviderActivityGeneratedAt = "-";
        ProviderHealthPlaidSyncAt = "-";
        ProviderHealthBalanceRefreshAt = "-";
        ProviderHealthDriftSummary = "Drift metrics unavailable.";
        ProviderHealthWebhookSummary = "No Stripe webhook events recorded.";
        ProviderHealthNotificationSummary = "Notification dispatch status unavailable.";
        ProviderHealthNotificationError = "-";
        ProviderHealthTraceId = "-";
        ProviderTimelineSummary = "No provider timeline events loaded.";
        NotificationRetrySummary = "No failed notification dispatch events loaded.";
        StripeWebhookSimulationSummary = "No webhook simulation has been run.";
        ProviderSecretRewrapSummary = "Provider secret rewrap has not been run.";

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        RefreshStatusCommand = new AsyncRelayCommand(RefreshStatusAsync);
        RefreshProviderActivityCommand = new AsyncRelayCommand(RefreshProviderActivityAsync);
        RefreshProviderTimelineCommand = new AsyncRelayCommand(RefreshProviderTimelineAsync);
        ClearProviderTimelineFiltersCommand = new AsyncRelayCommand(ClearProviderTimelineFiltersAsync);
        RefreshFailedNotificationEventsCommand = new AsyncRelayCommand(RefreshFailedNotificationEventsAsync);
        RetrySelectedFailedNotificationDispatchEventCommand = new AsyncRelayCommand(RetrySelectedFailedNotificationDispatchEventAsync);
        ReplaySelectedTimelineNotificationDispatchEventCommand = new AsyncRelayCommand(ReplaySelectedTimelineNotificationDispatchEventAsync);
        SimulateStripeWebhookCommand = new AsyncRelayCommand(SimulateStripeWebhookAsync);
        RewrapProviderSecretsCommand = new AsyncRelayCommand(RewrapProviderSecretsAsync);
        SaveNotificationPreferenceCommand = new AsyncRelayCommand(SaveNotificationPreferenceAsync);
        CreatePlaidLinkTokenCommand = new AsyncRelayCommand(CreatePlaidLinkTokenAsync);
        LaunchNativePlaidLinkCommand = new AsyncRelayCommand(LaunchNativePlaidLinkAsync);
        TogglePlaidLinkTokenVisibilityCommand = new RelayCommand(TogglePlaidLinkTokenVisibility);
        CopyPlaidLinkTokenCommand = new RelayCommand(CopyPlaidLinkToken);
        ToggleStripeClientSecretVisibilityCommand = new RelayCommand(ToggleStripeClientSecretVisibility);
        CopyStripeClientSecretCommand = new RelayCommand(CopyStripeClientSecret);
        CopyProviderTraceIdCommand = new RelayCommand(CopyProviderTraceId);
        ToggleProviderIdentifierVisibilityCommand = new RelayCommand(ToggleProviderIdentifierVisibility);
        ExchangePlaidPublicTokenCommand = new AsyncRelayCommand(ExchangePlaidPublicTokenAsync);
        UpsertPlaidAccountLinkCommand = new AsyncRelayCommand(UpsertPlaidAccountLinkAsync);
        DeleteSelectedPlaidAccountLinkCommand = new AsyncRelayCommand(DeleteSelectedPlaidAccountLinkAsync);
        SyncPlaidTransactionsCommand = new AsyncRelayCommand(SyncPlaidTransactionsAsync);
        RefreshPlaidBalancesCommand = new AsyncRelayCommand(RefreshPlaidBalancesAsync);
        LoadPlaidReconciliationCommand = new AsyncRelayCommand(LoadPlaidReconciliationAsync);
        CreateStripeSetupIntentCommand = new AsyncRelayCommand(CreateStripeSetupIntentAsync);
        RefreshFinancialAccountsCommand = new AsyncRelayCommand(RefreshFinancialAccountsAsync);
        RefreshSelectedEnvelopeCommand = new AsyncRelayCommand(RefreshSelectedEnvelopeAsync);
        LinkStripeFinancialAccountCommand = new AsyncRelayCommand(LinkStripeFinancialAccountAsync);
        IssueVirtualCardCommand = new AsyncRelayCommand(IssueVirtualCardAsync);
        IssuePhysicalCardCommand = new AsyncRelayCommand(IssuePhysicalCardAsync);
        FreezeSelectedCardCommand = new AsyncRelayCommand(FreezeSelectedCardAsync);
        UnfreezeSelectedCardCommand = new AsyncRelayCommand(UnfreezeSelectedCardAsync);
        CancelSelectedCardCommand = new AsyncRelayCommand(CancelSelectedCardAsync);
        RefreshCardIssuanceCommand = new AsyncRelayCommand(RefreshCardIssuanceAsync);
        SaveCardControlsCommand = new AsyncRelayCommand(SaveCardControlsAsync);
        EvaluateCardSpendCommand = new AsyncRelayCommand(EvaluateCardSpendAsync);

        _ = LoadCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadCommand { get; }
    public IAsyncRelayCommand RefreshStatusCommand { get; }
    public IAsyncRelayCommand RefreshProviderActivityCommand { get; }
    public IAsyncRelayCommand RefreshProviderTimelineCommand { get; }
    public IAsyncRelayCommand ClearProviderTimelineFiltersCommand { get; }
    public IAsyncRelayCommand RefreshFailedNotificationEventsCommand { get; }
    public IAsyncRelayCommand RetrySelectedFailedNotificationDispatchEventCommand { get; }
    public IAsyncRelayCommand ReplaySelectedTimelineNotificationDispatchEventCommand { get; }
    public IAsyncRelayCommand SimulateStripeWebhookCommand { get; }
    public IAsyncRelayCommand RewrapProviderSecretsCommand { get; }
    public IAsyncRelayCommand SaveNotificationPreferenceCommand { get; }
    public IAsyncRelayCommand CreatePlaidLinkTokenCommand { get; }
    public IAsyncRelayCommand LaunchNativePlaidLinkCommand { get; }
    public IRelayCommand TogglePlaidLinkTokenVisibilityCommand { get; }
    public IRelayCommand CopyPlaidLinkTokenCommand { get; }
    public IRelayCommand ToggleStripeClientSecretVisibilityCommand { get; }
    public IRelayCommand CopyStripeClientSecretCommand { get; }
    public IRelayCommand CopyProviderTraceIdCommand { get; }
    public IRelayCommand ToggleProviderIdentifierVisibilityCommand { get; }
    public IAsyncRelayCommand ExchangePlaidPublicTokenCommand { get; }
    public IAsyncRelayCommand UpsertPlaidAccountLinkCommand { get; }
    public IAsyncRelayCommand DeleteSelectedPlaidAccountLinkCommand { get; }
    public IAsyncRelayCommand SyncPlaidTransactionsCommand { get; }
    public IAsyncRelayCommand RefreshPlaidBalancesCommand { get; }
    public IAsyncRelayCommand LoadPlaidReconciliationCommand { get; }
    public IAsyncRelayCommand CreateStripeSetupIntentCommand { get; }
    public IAsyncRelayCommand RefreshFinancialAccountsCommand { get; }
    public IAsyncRelayCommand RefreshSelectedEnvelopeCommand { get; }
    public IAsyncRelayCommand LinkStripeFinancialAccountCommand { get; }
    public IAsyncRelayCommand IssueVirtualCardCommand { get; }
    public IAsyncRelayCommand IssuePhysicalCardCommand { get; }
    public IAsyncRelayCommand FreezeSelectedCardCommand { get; }
    public IAsyncRelayCommand UnfreezeSelectedCardCommand { get; }
    public IAsyncRelayCommand CancelSelectedCardCommand { get; }
    public IAsyncRelayCommand RefreshCardIssuanceCommand { get; }
    public IAsyncRelayCommand SaveCardControlsCommand { get; }
    public IAsyncRelayCommand EvaluateCardSpendCommand { get; }
    public IReadOnlyList<string> ProviderTimelineSourceFilterOptions { get; } =
    [
        "All Sources",
        "Stripe Webhooks",
        "Notification Dispatch"
    ];

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Financial integrations ready.";

    [ObservableProperty]
    private bool plaidConnected;

    [ObservableProperty]
    private bool stripeConnected;

    [ObservableProperty]
    private string plaidItemIdentifier = "-";

    [ObservableProperty]
    private string stripeCustomerIdentifier = "-";

    [ObservableProperty]
    private string financialStatusUpdatedAt = "-";

    [ObservableProperty]
    private bool showProviderIdentifiers;

    [ObservableProperty]
    private bool notificationEmailEnabled;

    [ObservableProperty]
    private bool notificationInAppEnabled = true;

    [ObservableProperty]
    private bool notificationSmsEnabled;

    [ObservableProperty]
    private string plaidClientName = string.Empty;

    [ObservableProperty]
    private string plaidLinkToken = string.Empty;

    [ObservableProperty]
    private bool showPlaidLinkToken;

    [ObservableProperty]
    private string plaidLinkTokenExpiresAt = "-";

    [ObservableProperty]
    private string plaidPublicToken = string.Empty;

    [ObservableProperty]
    private string plaidSyncSummary = string.Empty;

    [ObservableProperty]
    private string plaidBalanceRefreshSummary = string.Empty;

    [ObservableProperty]
    private string plaidReconciliationGeneratedAt = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PlaidReconciliationAccountItemViewModel> plaidReconciliationAccounts = [];

    [ObservableProperty]
    private ObservableCollection<AccountListItemViewModel> availableAccounts = [];

    [ObservableProperty]
    private AccountListItemViewModel? selectedAccount;

    [ObservableProperty]
    private string plaidAccountId = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PlaidAccountLinkItemViewModel> plaidAccountLinks = [];

    [ObservableProperty]
    private PlaidAccountLinkItemViewModel? selectedPlaidAccountLink;

    [ObservableProperty]
    private string stripeSetupEmail = string.Empty;

    [ObservableProperty]
    private string stripeSetupName = string.Empty;

    [ObservableProperty]
    private string stripeSetupIntentId = string.Empty;

    [ObservableProperty]
    private string stripeSetupCustomerId = string.Empty;

    [ObservableProperty]
    private string stripeClientSecret = string.Empty;

    [ObservableProperty]
    private bool showStripeClientSecret;

    [ObservableProperty]
    private ObservableCollection<EnvelopeOptionViewModel> availableEnvelopes = [];

    [ObservableProperty]
    private EnvelopeOptionViewModel? selectedEnvelope;

    [ObservableProperty]
    private string selectedEnvelopeSummary = string.Empty;

    [ObservableProperty]
    private string stripeFinancialAccountDisplayName = string.Empty;

    [ObservableProperty]
    private string envelopeFinancialAccountSummary = string.Empty;

    [ObservableProperty]
    private ObservableCollection<EnvelopeFinancialAccountItemViewModel> familyFinancialAccounts = [];

    [ObservableProperty]
    private ObservableCollection<PaymentCardItemViewModel> cards = [];

    [ObservableProperty]
    private PaymentCardItemViewModel? selectedCard;

    [ObservableProperty]
    private string virtualCardholderName = string.Empty;

    [ObservableProperty]
    private string physicalCardholderName = string.Empty;

    [ObservableProperty]
    private string shipmentRecipientName = string.Empty;

    [ObservableProperty]
    private string shipmentAddressLine1 = string.Empty;

    [ObservableProperty]
    private string shipmentAddressLine2 = string.Empty;

    [ObservableProperty]
    private string shipmentCity = string.Empty;

    [ObservableProperty]
    private string shipmentStateOrProvince = string.Empty;

    [ObservableProperty]
    private string shipmentPostalCode = string.Empty;

    [ObservableProperty]
    private string shipmentCountryCode = string.Empty;

    [ObservableProperty]
    private string issuanceSummary = string.Empty;

    [ObservableProperty]
    private string cardControlDailyLimit = string.Empty;

    [ObservableProperty]
    private string cardControlAllowedCategories = string.Empty;

    [ObservableProperty]
    private string cardControlAllowedMerchants = string.Empty;

    [ObservableProperty]
    private string cardControlSummary = string.Empty;

    [ObservableProperty]
    private ObservableCollection<CardControlAuditItemViewModel> cardControlAuditEntries = [];

    [ObservableProperty]
    private string cardSpendMerchantName = string.Empty;

    [ObservableProperty]
    private string cardSpendMerchantCategory = string.Empty;

    [ObservableProperty]
    private string cardSpendAmount = "0";

    [ObservableProperty]
    private string cardSpendTodayAmount = "0";

    [ObservableProperty]
    private string cardSpendEvaluationResult = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SecurityAuditEventItemViewModel> securityAuditEvents = [];

    [ObservableProperty]
    private string providerHealthStatusSummary = string.Empty;

    [ObservableProperty]
    private string providerActivityGeneratedAt = string.Empty;

    [ObservableProperty]
    private string providerHealthPlaidSyncAt = string.Empty;

    [ObservableProperty]
    private string providerHealthBalanceRefreshAt = string.Empty;

    [ObservableProperty]
    private string providerHealthDriftSummary = string.Empty;

    [ObservableProperty]
    private string providerHealthWebhookSummary = string.Empty;

    [ObservableProperty]
    private string providerHealthNotificationSummary = string.Empty;

    [ObservableProperty]
    private string providerHealthNotificationError = string.Empty;

    [ObservableProperty]
    private bool providerHealthNotificationHasError;

    [ObservableProperty]
    private string providerHealthTraceId = string.Empty;

    [ObservableProperty]
    private string providerTimelineSummary = string.Empty;

    [ObservableProperty]
    private string selectedProviderTimelineSourceFilter = "All Sources";

    [ObservableProperty]
    private string providerTimelineStatusFilter = string.Empty;

    [ObservableProperty]
    private string providerTimelineTake = "25";

    [ObservableProperty]
    private ObservableCollection<ProviderTimelineEventItemViewModel> providerTimelineEvents = [];

    [ObservableProperty]
    private ProviderTimelineEventItemViewModel? selectedProviderTimelineEvent;

    [ObservableProperty]
    private string notificationRetrySummary = string.Empty;

    [ObservableProperty]
    private ObservableCollection<FailedNotificationDispatchEventItemViewModel> failedNotificationDispatchEvents = [];

    [ObservableProperty]
    private FailedNotificationDispatchEventItemViewModel? selectedFailedNotificationDispatchEvent;

    [ObservableProperty]
    private string stripeWebhookSignature = string.Empty;

    [ObservableProperty]
    private string stripeWebhookPayload = string.Empty;

    [ObservableProperty]
    private string stripeWebhookSimulationSummary = string.Empty;

    [ObservableProperty]
    private string providerSecretRewrapSummary = string.Empty;

    public bool HasEnvelopeSelection => SelectedEnvelope is not null;
    public bool HasCardSelection => SelectedCard is not null;
    public bool HasFailedNotificationSelection => SelectedFailedNotificationDispatchEvent is not null;
    public bool CanReplaySelectedProviderTimelineEvent => SelectedProviderTimelineEvent?.CanReplayNotification == true;
    public bool SelectedCardIsPhysical => SelectedCard?.IsPhysical == true;
    public string PlaidItemIdentifierDisplay => GetMaskedIdentifierDisplay(PlaidItemIdentifier);
    public string StripeCustomerIdentifierDisplay => GetMaskedIdentifierDisplay(StripeCustomerIdentifier);
    public string StripeSetupIntentIdDisplay => GetMaskedIdentifierDisplay(StripeSetupIntentId);
    public string StripeSetupCustomerIdDisplay => GetMaskedIdentifierDisplay(StripeSetupCustomerId);
    public string PlaidLinkTokenDisplay => ShowPlaidLinkToken
        ? NormalizeSensitiveValue(PlaidLinkToken)
        : SensitiveValueMasker.MaskToken(PlaidLinkToken);
    public string StripeClientSecretDisplay => ShowStripeClientSecret
        ? NormalizeSensitiveValue(StripeClientSecret)
        : SensitiveValueMasker.MaskToken(StripeClientSecret);
    public string ProviderIdentifierVisibilityLabel => ShowProviderIdentifiers ? "Hide IDs" : "Reveal IDs";
    public string PlaidLinkTokenVisibilityLabel => ShowPlaidLinkToken ? "Hide Token" : "Reveal Token";
    public string StripeClientSecretVisibilityLabel => ShowStripeClientSecret ? "Hide Secret" : "Reveal Secret";

    partial void OnShowProviderIdentifiersChanged(bool value)
    {
        OnPropertyChanged(nameof(ProviderIdentifierVisibilityLabel));
        OnPropertyChanged(nameof(PlaidItemIdentifierDisplay));
        OnPropertyChanged(nameof(StripeCustomerIdentifierDisplay));
        OnPropertyChanged(nameof(StripeSetupIntentIdDisplay));
        OnPropertyChanged(nameof(StripeSetupCustomerIdDisplay));
        RecordSecurityEvent(value
            ? "Provider identifier reveal enabled."
            : "Provider identifier reveal disabled.");
    }

    partial void OnShowPlaidLinkTokenChanged(bool value)
    {
        OnPropertyChanged(nameof(PlaidLinkTokenDisplay));
        OnPropertyChanged(nameof(PlaidLinkTokenVisibilityLabel));
        RecordSecurityEvent(value
            ? "Plaid link token reveal enabled."
            : "Plaid link token reveal disabled.");
    }

    partial void OnShowStripeClientSecretChanged(bool value)
    {
        OnPropertyChanged(nameof(StripeClientSecretDisplay));
        OnPropertyChanged(nameof(StripeClientSecretVisibilityLabel));
        RecordSecurityEvent(value
            ? "Stripe client secret reveal enabled."
            : "Stripe client secret reveal disabled.");
    }

    partial void OnPlaidItemIdentifierChanged(string value) => OnPropertyChanged(nameof(PlaidItemIdentifierDisplay));
    partial void OnStripeCustomerIdentifierChanged(string value) => OnPropertyChanged(nameof(StripeCustomerIdentifierDisplay));
    partial void OnStripeSetupIntentIdChanged(string value) => OnPropertyChanged(nameof(StripeSetupIntentIdDisplay));
    partial void OnStripeSetupCustomerIdChanged(string value) => OnPropertyChanged(nameof(StripeSetupCustomerIdDisplay));
    partial void OnPlaidLinkTokenChanged(string value) => OnPropertyChanged(nameof(PlaidLinkTokenDisplay));
    partial void OnStripeClientSecretChanged(string value) => OnPropertyChanged(nameof(StripeClientSecretDisplay));

    partial void OnSelectedEnvelopeChanged(EnvelopeOptionViewModel? value)
    {
        OnPropertyChanged(nameof(HasEnvelopeSelection));
        if (_isApplyingEnvelopeSelection)
        {
            return;
        }

        _ = RefreshSelectedEnvelopeCommand.ExecuteAsync(null);
    }

    partial void OnSelectedCardChanged(PaymentCardItemViewModel? value)
    {
        OnPropertyChanged(nameof(HasCardSelection));
        OnPropertyChanged(nameof(SelectedCardIsPhysical));
        if (_isApplyingCardSelection)
        {
            return;
        }

        _ = LoadCardScopedDetailsAsync();
    }

    partial void OnSelectedFailedNotificationDispatchEventChanged(FailedNotificationDispatchEventItemViewModel? value)
    {
        OnPropertyChanged(nameof(HasFailedNotificationSelection));
    }

    partial void OnSelectedProviderTimelineEventChanged(ProviderTimelineEventItemViewModel? value)
    {
        OnPropertyChanged(nameof(CanReplaySelectedProviderTimelineEvent));
    }

    private Task LoadAsync(CancellationToken cancellationToken)
    {
        return RunOperationAsync("Loading financial integrations...", async ct =>
        {
            await RefreshStatusCoreAsync(ct);
            await LoadNotificationPreferenceCoreAsync(ct);
            await LoadAccountsAndEnvelopesCoreAsync(ct);
            await LoadPlaidAccountLinksCoreAsync(ct);
            await LoadFamilyFinancialAccountsCoreAsync(ct);
            await LoadSelectedEnvelopeCoreAsync(ct);
            await LoadPlaidReconciliationCoreAsync(ct);
            await LoadProviderActivityHealthCoreAsync(ct);
            await LoadProviderTimelineCoreAsync(ct);
            await LoadFailedNotificationDispatchEventsCoreAsync(ct);
            StatusMessage = "Financial integrations loaded.";
        }, cancellationToken);
    }

    private Task RefreshStatusAsync(CancellationToken cancellationToken)
    {
        return RunOperationAsync("Refreshing integration status...", async ct =>
        {
            await RefreshStatusCoreAsync(ct);
            await LoadNotificationPreferenceCoreAsync(ct);
            await LoadProviderActivityHealthCoreAsync(ct);
            await LoadProviderTimelineCoreAsync(ct);
            await LoadFailedNotificationDispatchEventsCoreAsync(ct);
            StatusMessage = "Integration status refreshed.";
        }, cancellationToken);
    }

    private Task RefreshProviderActivityAsync(CancellationToken cancellationToken)
    {
        return RunOperationAsync("Refreshing provider activity health...", async ct =>
        {
            await LoadProviderActivityHealthCoreAsync(ct);
            await LoadProviderTimelineCoreAsync(ct);
            await LoadFailedNotificationDispatchEventsCoreAsync(ct);
            StatusMessage = "Provider activity health refreshed.";
        }, cancellationToken);
    }

    private Task RefreshProviderTimelineAsync(CancellationToken cancellationToken)
    {
        if (!TryNormalizeProviderTimelineTake(strictValidation: true, out _))
        {
            return Task.CompletedTask;
        }

        return RunOperationAsync("Refreshing provider activity timeline...", async ct =>
        {
            await LoadProviderTimelineCoreAsync(ct, strictTakeValidation: true);
            await LoadFailedNotificationDispatchEventsCoreAsync(ct);
            StatusMessage = "Provider activity timeline refreshed.";
        }, cancellationToken);
    }

    private Task ClearProviderTimelineFiltersAsync(CancellationToken cancellationToken)
    {
        return RunOperationAsync("Clearing provider activity timeline filters...", async ct =>
        {
            SelectedProviderTimelineSourceFilter = "All Sources";
            ProviderTimelineStatusFilter = string.Empty;
            ProviderTimelineTake = "25";
            await LoadProviderTimelineCoreAsync(ct);
            await LoadFailedNotificationDispatchEventsCoreAsync(ct);
            StatusMessage = "Provider activity timeline filters cleared.";
        }, cancellationToken);
    }

    private Task RefreshFailedNotificationEventsAsync(CancellationToken cancellationToken)
    {
        return RunOperationAsync("Refreshing failed notification dispatch events...", async ct =>
        {
            await LoadFailedNotificationDispatchEventsCoreAsync(ct);
            StatusMessage = "Failed notification dispatch events refreshed.";
        }, cancellationToken);
    }

    private Task SaveNotificationPreferenceAsync(CancellationToken cancellationToken)
    {
        return RunOperationAsync("Saving notification preferences...", async ct =>
        {
            var updated = await _financialIntegrationDataService.UpdateNotificationPreferenceAsync(
                NotificationEmailEnabled,
                NotificationInAppEnabled,
                NotificationSmsEnabled,
                ct);
            ApplyNotificationPreference(updated);
            StatusMessage = "Notification preferences saved.";
        }, cancellationToken);
    }

    private Task RewrapProviderSecretsAsync(CancellationToken cancellationToken)
    {
        return RunOperationAsync("Rewrapping provider secrets...", async ct =>
        {
            var result = await _financialIntegrationDataService.RewrapProviderSecretsAsync(ct);
            var outcome = result.ProfileFound
                ? $"Profile found; fields touched {result.FieldsTouched}"
                : "No financial profile exists for this family";
            ProviderSecretRewrapSummary = $"{outcome}. Executed {FormatDate(result.ExecutedAtUtc)}.";

            await RefreshStatusCoreAsync(ct);
            StatusMessage = "Provider secret rewrap completed.";
        }, cancellationToken);
    }

    private async Task RetrySelectedFailedNotificationDispatchEventAsync(CancellationToken cancellationToken)
    {
        if (SelectedFailedNotificationDispatchEvent is null)
        {
            SetValidationError("Select a failed notification dispatch event to retry.");
            return;
        }

        await RunOperationAsync("Retrying failed notification dispatch event...", async ct =>
        {
            var retry = await _financialIntegrationDataService.RetryFailedNotificationDispatchEventAsync(
                SelectedFailedNotificationDispatchEvent.Id,
                ct);
            var attemptedAt = retry.LastAttemptAtUtc.HasValue
                ? FormatDate(retry.LastAttemptAtUtc.Value)
                : "-";
            var error = string.IsNullOrWhiteSpace(retry.ErrorMessage)
                ? "-"
                : retry.ErrorMessage;
            var retrySummary = $"Retry result: {retry.Status} (attempt {retry.AttemptCount}) at {attemptedAt}. Last error: {error}.";

            await LoadProviderActivityHealthCoreAsync(ct);
            await LoadProviderTimelineCoreAsync(ct);
            await LoadFailedNotificationDispatchEventsCoreAsync(ct);
            NotificationRetrySummary = retrySummary;
            StatusMessage = retry.Status.Equals("Sent", StringComparison.OrdinalIgnoreCase)
                ? "Notification dispatch retried successfully."
                : "Notification dispatch retry completed with failure status.";
        }, cancellationToken);
    }

    private async Task ReplaySelectedTimelineNotificationDispatchEventAsync(CancellationToken cancellationToken)
    {
        if (SelectedProviderTimelineEvent is null || !SelectedProviderTimelineEvent.CanReplayNotification)
        {
            SetValidationError("Select a replayable failed notification dispatch event in timeline.");
            return;
        }

        if (!SelectedProviderTimelineEvent.NotificationDispatchEventId.HasValue)
        {
            SetValidationError("Selected timeline event is missing notification event id.");
            return;
        }

        await RunOperationAsync("Replaying selected timeline notification event...", async ct =>
        {
            var replay = await _financialIntegrationDataService.ReplayTimelineNotificationDispatchEventAsync(
                SelectedProviderTimelineEvent.NotificationDispatchEventId.Value,
                ct);
            var attemptedAt = replay.LastAttemptAtUtc.HasValue
                ? FormatDate(replay.LastAttemptAtUtc.Value)
                : "-";
            var error = string.IsNullOrWhiteSpace(replay.ErrorMessage)
                ? "-"
                : replay.ErrorMessage;
            var replaySummary = $"Timeline replay result: {replay.Status} (attempt {replay.AttemptCount}) at {attemptedAt}. Last error: {error}.";

            await LoadProviderActivityHealthCoreAsync(ct);
            await LoadProviderTimelineCoreAsync(ct);
            await LoadFailedNotificationDispatchEventsCoreAsync(ct);
            NotificationRetrySummary = replaySummary;
            StatusMessage = replay.Status.Equals("Sent", StringComparison.OrdinalIgnoreCase)
                ? "Timeline notification replay completed successfully."
                : "Timeline notification replay completed with failure status.";
        }, cancellationToken);
    }

    private async Task SimulateStripeWebhookAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(StripeWebhookPayload))
        {
            SetValidationError("Stripe webhook payload is required.");
            return;
        }

        await RunOperationAsync("Processing Stripe webhook simulation...", async ct =>
        {
            var result = await _financialIntegrationDataService.ProcessStripeWebhookAsync(
                StripeWebhookPayload.Trim(),
                string.IsNullOrWhiteSpace(StripeWebhookSignature) ? null : StripeWebhookSignature.Trim(),
                ct);

            StripeWebhookSimulationSummary = FormatStripeWebhookSimulationSummary(result);
            await LoadProviderActivityHealthCoreAsync(ct);
            await LoadProviderTimelineCoreAsync(ct);
            await LoadFailedNotificationDispatchEventsCoreAsync(ct);
            StatusMessage = "Stripe webhook simulation processed.";
        }, cancellationToken);
    }

    private void TogglePlaidLinkTokenVisibility()
    {
        ShowPlaidLinkToken = !ShowPlaidLinkToken;
    }

    private void CopyPlaidLinkToken()
    {
        CopySensitiveValue(PlaidLinkToken, "Plaid link token copied to clipboard.");
    }

    private void ToggleStripeClientSecretVisibility()
    {
        ShowStripeClientSecret = !ShowStripeClientSecret;
    }

    private void CopyStripeClientSecret()
    {
        CopySensitiveValue(StripeClientSecret, "Stripe client secret copied to clipboard.");
    }

    private void CopyProviderTraceId()
    {
        if (string.IsNullOrWhiteSpace(ProviderHealthTraceId) || ProviderHealthTraceId == "-")
        {
            SetValidationError("No provider trace id is available to copy.");
            return;
        }

        try
        {
            Clipboard.SetText(ProviderHealthTraceId);
            RecordSecurityEvent("Provider activity trace id copied.");
            StatusMessage = "Provider trace id copied to clipboard.";
        }
        catch (Exception ex)
        {
            SetValidationError($"Clipboard copy failed: {ex.Message}");
        }
    }

    private void ToggleProviderIdentifierVisibility()
    {
        ShowProviderIdentifiers = !ShowProviderIdentifiers;
    }

    private Task CreatePlaidLinkTokenAsync(CancellationToken cancellationToken)
    {
        return RunOperationAsync("Creating Plaid link token...", async ct =>
        {
            var token = await _financialIntegrationDataService.CreatePlaidLinkTokenAsync(
                string.IsNullOrWhiteSpace(PlaidClientName) ? null : PlaidClientName.Trim(),
                ct);
            PlaidLinkToken = token.LinkToken;
            PlaidLinkTokenExpiresAt = FormatDate(token.ExpiresAtUtc);
            StatusMessage = "Plaid link token created.";
        }, cancellationToken);
    }

    private Task LaunchNativePlaidLinkAsync(CancellationToken cancellationToken)
    {
        return RunOperationAsync("Launching native Plaid Link...", async ct =>
        {
            if (string.IsNullOrWhiteSpace(PlaidLinkToken))
            {
                var token = await _financialIntegrationDataService.CreatePlaidLinkTokenAsync(
                    string.IsNullOrWhiteSpace(PlaidClientName) ? null : PlaidClientName.Trim(),
                    ct);
                PlaidLinkToken = token.LinkToken;
                PlaidLinkTokenExpiresAt = FormatDate(token.ExpiresAtUtc);
            }

            var linkResult = await _desktopPlaidLinkService.LaunchAsync(PlaidLinkToken, ct);
            if (linkResult.IsCanceled)
            {
                StatusMessage = linkResult.Message;
                return;
            }

            if (!linkResult.Succeeded || string.IsNullOrWhiteSpace(linkResult.PublicToken))
            {
                throw new InvalidOperationException(linkResult.Message);
            }

            // Keep token visibility brief; exchange immediately then clear.
            PlaidPublicToken = linkResult.PublicToken;
            var status = await _financialIntegrationDataService.ExchangePlaidPublicTokenAsync(linkResult.PublicToken, ct);
            ApplyFinancialStatus(status);
            PlaidPublicToken = string.Empty;
            PlaidLinkToken = string.Empty;
            PlaidLinkTokenExpiresAt = "-";
            StatusMessage = "Plaid Link completed and token exchanged.";
        }, cancellationToken);
    }

    private async Task ExchangePlaidPublicTokenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(PlaidPublicToken))
        {
            SetValidationError("Plaid public token is required.");
            return;
        }

        await RunOperationAsync("Exchanging Plaid public token...", async ct =>
        {
            var status = await _financialIntegrationDataService.ExchangePlaidPublicTokenAsync(PlaidPublicToken.Trim(), ct);
            ApplyFinancialStatus(status);
            PlaidPublicToken = string.Empty;
            StatusMessage = "Plaid public token exchanged.";
        }, cancellationToken);
    }

    private async Task UpsertPlaidAccountLinkAsync(CancellationToken cancellationToken)
    {
        if (SelectedAccount is null)
        {
            SetValidationError("Select a local account before linking Plaid.");
            return;
        }

        if (string.IsNullOrWhiteSpace(PlaidAccountId))
        {
            SetValidationError("Plaid account id is required.");
            return;
        }

        await RunOperationAsync("Saving Plaid account link...", async ct =>
        {
            await _financialIntegrationDataService.UpsertPlaidAccountLinkAsync(
                SelectedAccount.Id,
                PlaidAccountId.Trim(),
                ct);
            PlaidAccountId = string.Empty;
            await LoadPlaidAccountLinksCoreAsync(ct);
            StatusMessage = "Plaid account link saved.";
        }, cancellationToken);
    }

    private async Task DeleteSelectedPlaidAccountLinkAsync(CancellationToken cancellationToken)
    {
        if (SelectedPlaidAccountLink is null)
        {
            SetValidationError("Select a Plaid link to delete.");
            return;
        }

        var confirmation = MessageBox.Show(
            $"Delete Plaid link for account '{SelectedPlaidAccountLink.AccountName}'?",
            "Delete Plaid Link",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.Yes)
        {
            StatusMessage = "Plaid link deletion canceled.";
            return;
        }

        await RunOperationAsync("Deleting Plaid account link...", async ct =>
        {
            await _financialIntegrationDataService.DeletePlaidAccountLinkAsync(SelectedPlaidAccountLink.Id, ct);
            await LoadPlaidAccountLinksCoreAsync(ct);
            StatusMessage = "Plaid account link deleted.";
        }, cancellationToken);
    }

    private Task SyncPlaidTransactionsAsync(CancellationToken cancellationToken)
    {
        return RunOperationAsync("Syncing Plaid transactions...", async ct =>
        {
            var sync = await _financialIntegrationDataService.SyncPlaidTransactionsAsync(ct);
            PlaidSyncSummary =
                $"Pulled {sync.PulledCount}, inserted {sync.InsertedCount}, deduped {sync.DedupedCount}, unmapped {sync.UnmappedCount} at {FormatDate(sync.ProcessedAtUtc)}.";
            await LoadProviderActivityHealthCoreAsync(ct);
            await LoadProviderTimelineCoreAsync(ct);
            StatusMessage = "Plaid transaction sync completed.";
        }, cancellationToken);
    }

    private Task RefreshPlaidBalancesAsync(CancellationToken cancellationToken)
    {
        return RunOperationAsync("Refreshing Plaid balances...", async ct =>
        {
            var refresh = await _financialIntegrationDataService.RefreshPlaidBalancesAsync(ct);
            PlaidBalanceRefreshSummary =
                $"Refreshed {refresh.RefreshedCount} accounts, drifted {refresh.DriftedCount}, total drift {refresh.TotalAbsoluteDrift.ToString("$#,##0.00")} at {FormatDate(refresh.RefreshedAtUtc)}.";
            await LoadPlaidReconciliationCoreAsync(ct);
            await LoadProviderActivityHealthCoreAsync(ct);
            await LoadProviderTimelineCoreAsync(ct);
            StatusMessage = "Plaid balances refreshed.";
        }, cancellationToken);
    }

    private Task LoadPlaidReconciliationAsync(CancellationToken cancellationToken)
    {
        return RunOperationAsync("Loading Plaid reconciliation...", async ct =>
        {
            await LoadPlaidReconciliationCoreAsync(ct);
            StatusMessage = "Plaid reconciliation loaded.";
        }, cancellationToken);
    }

    private async Task CreateStripeSetupIntentAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(StripeSetupEmail) || !StripeSetupEmail.Contains('@', StringComparison.Ordinal))
        {
            SetValidationError("A valid Stripe customer email is required.");
            return;
        }

        await RunOperationAsync("Creating Stripe setup intent...", async ct =>
        {
            var setupIntent = await _financialIntegrationDataService.CreateStripeSetupIntentAsync(
                StripeSetupEmail.Trim(),
                string.IsNullOrWhiteSpace(StripeSetupName) ? null : StripeSetupName.Trim(),
                ct);
            StripeSetupCustomerId = setupIntent.CustomerId;
            StripeSetupIntentId = setupIntent.SetupIntentId;
            StripeClientSecret = setupIntent.ClientSecret;
            await RefreshStatusCoreAsync(ct);
            StatusMessage = "Stripe setup intent created.";
        }, cancellationToken);
    }

    private Task RefreshFinancialAccountsAsync(CancellationToken cancellationToken)
    {
        return RunOperationAsync("Refreshing family financial accounts...", async ct =>
        {
            await LoadFamilyFinancialAccountsCoreAsync(ct);
            StatusMessage = "Family financial accounts refreshed.";
        }, cancellationToken);
    }

    private Task RefreshSelectedEnvelopeAsync(CancellationToken cancellationToken)
    {
        return RunOperationAsync("Refreshing selected envelope financial data...", async ct =>
        {
            await LoadSelectedEnvelopeCoreAsync(ct);
            StatusMessage = "Envelope financial data refreshed.";
        }, cancellationToken);
    }

    private async Task LinkStripeFinancialAccountAsync(CancellationToken cancellationToken)
    {
        if (SelectedEnvelope is null)
        {
            SetValidationError("Select an envelope first.");
            return;
        }

        await RunOperationAsync("Linking Stripe financial account...", async ct =>
        {
            var account = await _financialIntegrationDataService.LinkStripeEnvelopeFinancialAccountAsync(
                SelectedEnvelope.Id,
                string.IsNullOrWhiteSpace(StripeFinancialAccountDisplayName) ? null : StripeFinancialAccountDisplayName.Trim(),
                ct);

            EnvelopeFinancialAccountSummary = $"Linked {account.Provider} account {SensitiveValueMasker.MaskIdentifier(account.ProviderFinancialAccountId)}.";
            await LoadFamilyFinancialAccountsCoreAsync(ct);
            await LoadSelectedEnvelopeCoreAsync(ct);
            StatusMessage = "Stripe financial account linked.";
        }, cancellationToken);
    }

    private async Task IssueVirtualCardAsync(CancellationToken cancellationToken)
    {
        if (SelectedEnvelope is null)
        {
            SetValidationError("Select an envelope before issuing a card.");
            return;
        }

        await RunOperationAsync("Issuing virtual card...", async ct =>
        {
            var card = await _financialIntegrationDataService.IssueVirtualCardAsync(
                SelectedEnvelope.Id,
                string.IsNullOrWhiteSpace(VirtualCardholderName) ? null : VirtualCardholderName.Trim(),
                ct);

            VirtualCardholderName = string.Empty;
            await LoadSelectedEnvelopeCoreAsync(ct);
            SelectCard(card.Id);
            StatusMessage = "Virtual card issued.";
        }, cancellationToken);
    }

    private async Task IssuePhysicalCardAsync(CancellationToken cancellationToken)
    {
        if (SelectedEnvelope is null)
        {
            SetValidationError("Select an envelope before issuing a physical card.");
            return;
        }

        if (string.IsNullOrWhiteSpace(ShipmentRecipientName)
            || string.IsNullOrWhiteSpace(ShipmentAddressLine1)
            || string.IsNullOrWhiteSpace(ShipmentCity)
            || string.IsNullOrWhiteSpace(ShipmentStateOrProvince)
            || string.IsNullOrWhiteSpace(ShipmentPostalCode)
            || string.IsNullOrWhiteSpace(ShipmentCountryCode))
        {
            SetValidationError("Recipient, address, city, state, postal code, and country are required for physical cards.");
            return;
        }

        if (ShipmentCountryCode.Trim().Length != 2)
        {
            SetValidationError("Country code must use ISO two-letter format (example: US).");
            return;
        }

        await RunOperationAsync("Issuing physical card...", async ct =>
        {
            var issuance = await _financialIntegrationDataService.IssuePhysicalCardAsync(
                SelectedEnvelope.Id,
                string.IsNullOrWhiteSpace(PhysicalCardholderName) ? null : PhysicalCardholderName.Trim(),
                ShipmentRecipientName.Trim(),
                ShipmentAddressLine1.Trim(),
                string.IsNullOrWhiteSpace(ShipmentAddressLine2) ? null : ShipmentAddressLine2.Trim(),
                ShipmentCity.Trim(),
                ShipmentStateOrProvince.Trim(),
                ShipmentPostalCode.Trim(),
                ShipmentCountryCode.Trim().ToUpperInvariant(),
                ct);

            IssuanceSummary = FormatIssuanceSummary(issuance);
            await LoadSelectedEnvelopeCoreAsync(ct);
            SelectCard(issuance.Card.Id);
            StatusMessage = "Physical card issuance requested.";
        }, cancellationToken);
    }

    private Task FreezeSelectedCardAsync(CancellationToken cancellationToken)
    {
        return UpdateSelectedCardAsync(
            "Freezing card...",
            (envelopeId, cardId, ct) => _financialIntegrationDataService.FreezeCardAsync(envelopeId, cardId, ct),
            "Card frozen.",
            cancellationToken);
    }

    private Task UnfreezeSelectedCardAsync(CancellationToken cancellationToken)
    {
        return UpdateSelectedCardAsync(
            "Unfreezing card...",
            (envelopeId, cardId, ct) => _financialIntegrationDataService.UnfreezeCardAsync(envelopeId, cardId, ct),
            "Card unfrozen.",
            cancellationToken);
    }

    private Task CancelSelectedCardAsync(CancellationToken cancellationToken)
    {
        return UpdateSelectedCardAsync(
            "Canceling card...",
            (envelopeId, cardId, ct) => _financialIntegrationDataService.CancelCardAsync(envelopeId, cardId, ct),
            "Card canceled.",
            cancellationToken);
    }

    private async Task RefreshCardIssuanceAsync(CancellationToken cancellationToken)
    {
        if (SelectedEnvelope is null || SelectedCard is null)
        {
            SetValidationError("Select a card to refresh issuance details.");
            return;
        }

        if (!SelectedCard.IsPhysical)
        {
            SetValidationError("Issuance tracking only applies to physical cards.");
            return;
        }

        await RunOperationAsync("Refreshing physical card issuance...", async ct =>
        {
            var issuance = await _financialIntegrationDataService.RefreshPhysicalCardIssuanceAsync(
                SelectedEnvelope.Id,
                SelectedCard.Id,
                ct);
            IssuanceSummary = FormatIssuanceSummary(issuance);
            await LoadSelectedEnvelopeCoreAsync(ct);
            SelectCard(issuance.Card.Id);
            StatusMessage = "Physical card issuance refreshed.";
        }, cancellationToken);
    }

    private async Task SaveCardControlsAsync(CancellationToken cancellationToken)
    {
        if (SelectedEnvelope is null || SelectedCard is null)
        {
            SetValidationError("Select a card before saving controls.");
            return;
        }

        decimal? dailyLimitAmount = null;
        if (!string.IsNullOrWhiteSpace(CardControlDailyLimit))
        {
            if (!decimal.TryParse(CardControlDailyLimit, out var parsedLimit) || parsedLimit < 0m)
            {
                SetValidationError("Daily limit must be a non-negative number.");
                return;
            }

            dailyLimitAmount = parsedLimit;
        }

        var categories = ParseDelimitedValues(CardControlAllowedCategories);
        var merchants = ParseDelimitedValues(CardControlAllowedMerchants);
        if (!dailyLimitAmount.HasValue && categories.Count == 0 && merchants.Count == 0)
        {
            SetValidationError("Specify at least one control value.");
            return;
        }

        await RunOperationAsync("Saving card controls...", async ct =>
        {
            var control = await _financialIntegrationDataService.UpsertCardControlsAsync(
                SelectedEnvelope.Id,
                SelectedCard.Id,
                dailyLimitAmount,
                categories,
                merchants,
                ct);

            ApplyCardControls(control);
            var audit = await _financialIntegrationDataService.ListCardControlAuditAsync(
                SelectedEnvelope.Id,
                SelectedCard.Id,
                ct);
            ApplyCardControlAudit(audit);
            StatusMessage = "Card controls saved.";
        }, cancellationToken);
    }

    private async Task EvaluateCardSpendAsync(CancellationToken cancellationToken)
    {
        if (SelectedEnvelope is null || SelectedCard is null)
        {
            SetValidationError("Select a card before evaluating spend.");
            return;
        }

        if (string.IsNullOrWhiteSpace(CardSpendMerchantName))
        {
            SetValidationError("Merchant name is required for spend evaluation.");
            return;
        }

        if (!decimal.TryParse(CardSpendAmount, out var amount) || amount <= 0m)
        {
            SetValidationError("Spend amount must be greater than zero.");
            return;
        }

        if (!decimal.TryParse(CardSpendTodayAmount, out var spentTodayAmount) || spentTodayAmount < 0m)
        {
            SetValidationError("Spent today must be a non-negative amount.");
            return;
        }

        await RunOperationAsync("Evaluating card spend...", async ct =>
        {
            var evaluation = await _financialIntegrationDataService.EvaluateCardSpendAsync(
                SelectedEnvelope.Id,
                SelectedCard.Id,
                CardSpendMerchantName.Trim(),
                string.IsNullOrWhiteSpace(CardSpendMerchantCategory) ? null : CardSpendMerchantCategory.Trim(),
                amount,
                spentTodayAmount,
                ct);

            CardSpendEvaluationResult = evaluation.IsAllowed
                ? evaluation.RemainingDailyLimit.HasValue
                    ? $"Allowed. Remaining daily limit: {evaluation.RemainingDailyLimit.Value.ToString("$#,##0.00")}."
                    : "Allowed."
                : $"Denied: {evaluation.DenialReason ?? "Rule denied spend."}";
            StatusMessage = "Card spend evaluation complete.";
        }, cancellationToken);
    }

    private async Task LoadCardScopedDetailsAsync()
    {
        await RunOperationAsync(
            "Loading selected card details...",
            LoadSelectedCardCoreAsync,
            CancellationToken.None);
    }

    private async Task UpdateSelectedCardAsync(
        string operationName,
        Func<Guid, Guid, CancellationToken, Task<EnvelopePaymentCardResponse>> action,
        string successMessage,
        CancellationToken cancellationToken)
    {
        if (SelectedEnvelope is null || SelectedCard is null)
        {
            SetValidationError("Select a card first.");
            return;
        }

        await RunOperationAsync(operationName, async ct =>
        {
            await action(SelectedEnvelope.Id, SelectedCard.Id, ct);
            await LoadSelectedEnvelopeCoreAsync(ct);
            StatusMessage = successMessage;
        }, cancellationToken);
    }

    private async Task RefreshStatusCoreAsync(CancellationToken cancellationToken)
    {
        var status = await _financialIntegrationDataService.GetFamilyFinancialStatusAsync(cancellationToken);
        ApplyFinancialStatus(status);
    }

    private async Task LoadNotificationPreferenceCoreAsync(CancellationToken cancellationToken)
    {
        var preference = await _financialIntegrationDataService.GetNotificationPreferenceAsync(cancellationToken);
        ApplyNotificationPreference(preference);
    }

    private async Task LoadAccountsAndEnvelopesCoreAsync(CancellationToken cancellationToken)
    {
        var accountsTask = _accountsDataService.GetAccountsAsync(cancellationToken);
        var envelopesTask = _envelopesDataService.GetEnvelopesAsync(cancellationToken);
        await Task.WhenAll(accountsTask, envelopesTask);

        var accountItems = (await accountsTask)
            .OrderBy(static account => account.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        AvailableAccounts = new ObservableCollection<AccountListItemViewModel>(accountItems);

        if (SelectedAccount is null || !AvailableAccounts.Any(account => account.Id == SelectedAccount.Id))
        {
            SelectedAccount = AvailableAccounts.FirstOrDefault();
        }

        var envelopeItems = (await envelopesTask)
            .Where(static envelope => !envelope.IsArchived)
            .OrderBy(static envelope => envelope.Name, StringComparer.OrdinalIgnoreCase)
            .Select(static envelope => new EnvelopeOptionViewModel(envelope.Id, envelope.Name))
            .ToArray();
        AvailableEnvelopes = new ObservableCollection<EnvelopeOptionViewModel>(envelopeItems);

        if (SelectedEnvelope is null || !AvailableEnvelopes.Any(envelope => envelope.Id == SelectedEnvelope.Id))
        {
            _isApplyingEnvelopeSelection = true;
            SelectedEnvelope = AvailableEnvelopes.FirstOrDefault();
            _isApplyingEnvelopeSelection = false;
            OnPropertyChanged(nameof(HasEnvelopeSelection));
        }
    }

    private async Task LoadPlaidAccountLinksCoreAsync(CancellationToken cancellationToken)
    {
        var previouslySelectedLinkId = SelectedPlaidAccountLink?.Id;
        var accountLookup = AvailableAccounts.ToDictionary(static account => account.Id, static account => account.Name);
        var links = await _financialIntegrationDataService.ListPlaidAccountLinksAsync(cancellationToken);

        PlaidAccountLinks = new ObservableCollection<PlaidAccountLinkItemViewModel>(
            links
                .OrderBy(link => accountLookup.TryGetValue(link.AccountId, out var name) ? name : link.AccountId.ToString(), StringComparer.OrdinalIgnoreCase)
                .Select(link => new PlaidAccountLinkItemViewModel(
                    link.Id,
                    link.AccountId,
                    accountLookup.TryGetValue(link.AccountId, out var accountName) ? accountName : link.AccountId.ToString(),
                    SensitiveValueMasker.MaskIdentifier(link.PlaidAccountId),
                    FormatDate(link.UpdatedAtUtc))));

        SelectedPlaidAccountLink = previouslySelectedLinkId.HasValue
            ? PlaidAccountLinks.FirstOrDefault(link => link.Id == previouslySelectedLinkId.Value)
            : PlaidAccountLinks.FirstOrDefault();
    }

    private async Task LoadFamilyFinancialAccountsCoreAsync(CancellationToken cancellationToken)
    {
        var envelopeLookup = AvailableEnvelopes.ToDictionary(static envelope => envelope.Id, static envelope => envelope.Name);
        var accounts = await _financialIntegrationDataService.ListFamilyFinancialAccountsAsync(cancellationToken);

        FamilyFinancialAccounts = new ObservableCollection<EnvelopeFinancialAccountItemViewModel>(
            accounts
                .OrderBy(account => envelopeLookup.TryGetValue(account.EnvelopeId, out var name) ? name : account.EnvelopeId.ToString(), StringComparer.OrdinalIgnoreCase)
                .Select(account => new EnvelopeFinancialAccountItemViewModel(
                    account.Id,
                    account.EnvelopeId,
                    envelopeLookup.TryGetValue(account.EnvelopeId, out var envelopeName) ? envelopeName : account.EnvelopeId.ToString(),
                    account.Provider,
                    SensitiveValueMasker.MaskIdentifier(account.ProviderFinancialAccountId),
                    FormatDate(account.UpdatedAtUtc))));
    }

    private async Task LoadSelectedEnvelopeCoreAsync(CancellationToken cancellationToken)
    {
        if (SelectedEnvelope is null)
        {
            ClearEnvelopeScopedState();
            return;
        }

        SelectedEnvelopeSummary = $"Managing envelope: {SelectedEnvelope.Name}";

        var financialAccountTask = _financialIntegrationDataService.GetEnvelopeFinancialAccountAsync(SelectedEnvelope.Id, cancellationToken);
        var cardsTask = _financialIntegrationDataService.ListEnvelopeCardsAsync(SelectedEnvelope.Id, cancellationToken);
        await Task.WhenAll(financialAccountTask, cardsTask);

        var account = await financialAccountTask;
        EnvelopeFinancialAccountSummary = account is null
            ? "No linked Stripe financial account."
            : $"Linked {account.Provider} account {SensitiveValueMasker.MaskIdentifier(account.ProviderFinancialAccountId)}.";

        var cardItems = (await cardsTask)
            .OrderByDescending(static card => card.CreatedAtUtc)
            .Select(MapCard)
            .ToArray();
        Cards = new ObservableCollection<PaymentCardItemViewModel>(cardItems);

        if (SelectedCard is null || !Cards.Any(card => card.Id == SelectedCard.Id))
        {
            _isApplyingCardSelection = true;
            SelectedCard = Cards.FirstOrDefault();
            _isApplyingCardSelection = false;
            OnPropertyChanged(nameof(HasCardSelection));
            OnPropertyChanged(nameof(SelectedCardIsPhysical));
        }

        await LoadSelectedCardCoreAsync(cancellationToken);
    }

    private async Task LoadSelectedCardCoreAsync(CancellationToken cancellationToken)
    {
        if (SelectedEnvelope is null || SelectedCard is null)
        {
            ClearSelectedCardState();
            return;
        }

        var controlsTask = _financialIntegrationDataService.GetCardControlsAsync(SelectedEnvelope.Id, SelectedCard.Id, cancellationToken);
        var auditTask = _financialIntegrationDataService.ListCardControlAuditAsync(SelectedEnvelope.Id, SelectedCard.Id, cancellationToken);
        var issuanceTask = SelectedCard.IsPhysical
            ? _financialIntegrationDataService.GetPhysicalCardIssuanceAsync(SelectedEnvelope.Id, SelectedCard.Id, cancellationToken)
            : Task.FromResult<EnvelopePhysicalCardIssuanceResponse?>(null);

        await Task.WhenAll(controlsTask, auditTask, issuanceTask);

        ApplyCardControls(await controlsTask);
        ApplyCardControlAudit(await auditTask);

        var issuance = await issuanceTask;
        IssuanceSummary = issuance is null
            ? SelectedCard.IsPhysical
                ? "No issuance status has been returned for this physical card."
                : "Selected card is virtual."
            : FormatIssuanceSummary(issuance);
    }

    private async Task LoadPlaidReconciliationCoreAsync(CancellationToken cancellationToken)
    {
        var report = await _financialIntegrationDataService.GetPlaidReconciliationReportAsync(cancellationToken);
        PlaidReconciliationGeneratedAt = FormatDate(report.GeneratedAtUtc);
        PlaidReconciliationAccounts = new ObservableCollection<PlaidReconciliationAccountItemViewModel>(
            report.Accounts
                .OrderByDescending(static account => Math.Abs(account.DriftAmount))
                .ThenBy(static account => account.AccountName, StringComparer.OrdinalIgnoreCase)
                .Select(static account => new PlaidReconciliationAccountItemViewModel(
                    account.AccountId,
                    account.AccountName,
                    account.PlaidAccountId,
                    account.InternalBalance.ToString("$#,##0.00"),
                    account.ProviderBalance.ToString("$#,##0.00"),
                    account.DriftAmount.ToString("$#,##0.00"),
                    account.IsDrifted)));
    }

    private async Task LoadProviderActivityHealthCoreAsync(CancellationToken cancellationToken)
    {
        var activity = await _financialIntegrationDataService.GetProviderActivityHealthAsync(cancellationToken);

        ProviderActivityGeneratedAt = FormatDate(activity.GeneratedAtUtc);
        ProviderHealthPlaidSyncAt = activity.LastPlaidTransactionSyncAtUtc.HasValue
            ? FormatDate(activity.LastPlaidTransactionSyncAtUtc.Value)
            : "-";
        ProviderHealthBalanceRefreshAt = activity.LastPlaidBalanceRefreshAtUtc.HasValue
            ? FormatDate(activity.LastPlaidBalanceRefreshAtUtc.Value)
            : "-";
        ProviderHealthDriftSummary = $"Drifted accounts: {activity.DriftedAccountCount}. Total drift: {activity.TotalAbsoluteDrift.ToString("$#,##0.00")}.";

        ProviderHealthWebhookSummary = activity.LastStripeWebhook is null
            ? "No Stripe webhook events recorded."
            : $"Last webhook {activity.LastStripeWebhook.EventType} -> {activity.LastStripeWebhook.ProcessingStatus} at {FormatDate(activity.LastStripeWebhook.ProcessedAtUtc)}.";

        var dispatch = activity.NotificationDispatch;
        var lastAttempt = dispatch.LastAttemptAtUtc.HasValue
            ? FormatDate(dispatch.LastAttemptAtUtc.Value)
            : "-";
        ProviderHealthNotificationSummary =
            $"Dispatch {dispatch.Status}. Queued {dispatch.QueuedCount}, sent {dispatch.SentCount}, failed {dispatch.FailedCount}. Last attempt {lastAttempt}.";

        ProviderHealthNotificationHasError = dispatch.FailedCount > 0
            || !string.IsNullOrWhiteSpace(dispatch.LastErrorMessage);
        ProviderHealthNotificationError = string.IsNullOrWhiteSpace(dispatch.LastErrorMessage)
            ? "-"
            : dispatch.LastErrorMessage;
        ProviderHealthTraceId = string.IsNullOrWhiteSpace(activity.TraceId)
            ? "-"
            : activity.TraceId.Trim();

        ProviderHealthStatusSummary = $"Generated {ProviderActivityGeneratedAt}.";
    }

    private async Task LoadProviderTimelineCoreAsync(CancellationToken cancellationToken, bool strictTakeValidation = false)
    {
        if (!TryNormalizeProviderTimelineTake(strictTakeValidation, out var take))
        {
            return;
        }

        var sourceFilter = ResolveProviderTimelineSourceFilter();
        var statusFilter = string.IsNullOrWhiteSpace(ProviderTimelineStatusFilter)
            ? null
            : ProviderTimelineStatusFilter.Trim();
        var timeline = await _financialIntegrationDataService.GetProviderActivityTimelineAsync(
            take: take,
            sourceFilter: sourceFilter,
            statusFilter: statusFilter,
            cancellationToken: cancellationToken);
        var previousSelectedEventId = SelectedProviderTimelineEvent?.NotificationDispatchEventId;

        ProviderTimelineEvents = new ObservableCollection<ProviderTimelineEventItemViewModel>(
            timeline.Events.Select(eventItem => new ProviderTimelineEventItemViewModel(
                eventItem.Source,
                eventItem.EventType,
                eventItem.Status,
                FormatDate(eventItem.OccurredAtUtc),
                eventItem.Summary,
                string.IsNullOrWhiteSpace(eventItem.Detail) ? "-" : eventItem.Detail,
                eventItem.NotificationDispatchEventId,
                eventItem.Source.Equals("NotificationDispatch", StringComparison.OrdinalIgnoreCase)
                    && eventItem.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase)
                    && eventItem.NotificationDispatchEventId.HasValue)));

        SelectedProviderTimelineEvent = previousSelectedEventId.HasValue
            ? ProviderTimelineEvents.FirstOrDefault(evt => evt.NotificationDispatchEventId == previousSelectedEventId.Value)
            : ProviderTimelineEvents.FirstOrDefault(evt => evt.CanReplayNotification);

        var sourceSummary = SelectedProviderTimelineSourceFilter;
        var statusSummary = statusFilter ?? "Any Status";
        ProviderTimelineSummary = timeline.Events.Count == 0
            ? $"No provider timeline events recorded for {sourceSummary} / {statusSummary}."
            : $"Showing {timeline.Events.Count} recent events (requested {timeline.RequestedTake}) for {sourceSummary} / {statusSummary}.";

        if (!string.IsNullOrWhiteSpace(timeline.TraceId))
        {
            ProviderHealthTraceId = timeline.TraceId.Trim();
        }
    }

    private bool TryNormalizeProviderTimelineTake(bool strictValidation, out int take)
    {
        if (string.IsNullOrWhiteSpace(ProviderTimelineTake))
        {
            ProviderTimelineTake = "25";
            take = 25;
            return true;
        }

        if (!int.TryParse(ProviderTimelineTake.Trim(), out var parsedTake))
        {
            if (strictValidation)
            {
                SetValidationError("Timeline take must be a whole number between 1 and 100.");
                take = 0;
                return false;
            }

            ProviderTimelineTake = "25";
            take = 25;
            return true;
        }

        if (parsedTake is < 1 or > 100)
        {
            if (strictValidation)
            {
                SetValidationError("Timeline take must be between 1 and 100.");
                take = 0;
                return false;
            }

            ProviderTimelineTake = "25";
            take = 25;
            return true;
        }

        take = parsedTake;
        return true;
    }

    private string? ResolveProviderTimelineSourceFilter()
    {
        return SelectedProviderTimelineSourceFilter switch
        {
            "Stripe Webhooks" => "StripeWebhook",
            "Notification Dispatch" => "NotificationDispatch",
            _ => null
        };
    }

    private async Task LoadFailedNotificationDispatchEventsCoreAsync(CancellationToken cancellationToken)
    {
        var previouslySelectedEventId = SelectedFailedNotificationDispatchEvent?.Id;
        var failedEvents = await _financialIntegrationDataService.ListFailedNotificationDispatchEventsAsync(
            cancellationToken: cancellationToken);

        FailedNotificationDispatchEvents = new ObservableCollection<FailedNotificationDispatchEventItemViewModel>(
            failedEvents.Select(static evt => new FailedNotificationDispatchEventItemViewModel(
                evt.Id,
                evt.Channel,
                evt.Merchant,
                evt.Amount.ToString("$#,##0.00"),
                evt.AttemptCount,
                evt.LastAttemptAtUtc.HasValue ? FormatDate(evt.LastAttemptAtUtc.Value) : "-",
                string.IsNullOrWhiteSpace(evt.ErrorMessage) ? "-" : evt.ErrorMessage)));

        SelectedFailedNotificationDispatchEvent = previouslySelectedEventId.HasValue
            ? FailedNotificationDispatchEvents.FirstOrDefault(evt => evt.Id == previouslySelectedEventId.Value)
            : FailedNotificationDispatchEvents.FirstOrDefault();

        NotificationRetrySummary = failedEvents.Count == 0
            ? "No failed notification dispatch events available for retry."
            : $"Showing {failedEvents.Count} failed notification dispatch events.";
    }

    private async Task RunOperationAsync(
        string operationDescription,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        StatusMessage = operationDescription;

        try
        {
            await operation(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            HasError = true;
            ErrorMessage = $"{operationDescription} canceled.";
            StatusMessage = ErrorMessage;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            StatusMessage = $"{operationDescription} failed.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SetValidationError(string message)
    {
        HasError = true;
        ErrorMessage = message;
        StatusMessage = message;
    }

    private void CopySensitiveValue(string? value, string auditMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            SetValidationError("No sensitive value is currently available to copy.");
            return;
        }

        try
        {
            Clipboard.SetText(value);
            RecordSecurityEvent(auditMessage);
            StatusMessage = "Value copied to clipboard.";
        }
        catch (Exception ex)
        {
            SetValidationError($"Clipboard copy failed: {ex.Message}");
        }
    }

    private void RecordSecurityEvent(string action)
    {
        SecurityAuditEvents.Insert(
            0,
            new SecurityAuditEventItemViewModel(
                Timestamp: FormatDate(DateTimeOffset.UtcNow),
                Action: action));

        while (SecurityAuditEvents.Count > 100)
        {
            SecurityAuditEvents.RemoveAt(SecurityAuditEvents.Count - 1);
        }
    }

    private void ApplyFinancialStatus(FamilyFinancialStatusResponse status)
    {
        PlaidConnected = status.PlaidConnected;
        StripeConnected = status.StripeConnected;
        PlaidItemIdentifier = string.IsNullOrWhiteSpace(status.PlaidItemId) ? "-" : status.PlaidItemId;
        StripeCustomerIdentifier = string.IsNullOrWhiteSpace(status.StripeCustomerId) ? "-" : status.StripeCustomerId;
        FinancialStatusUpdatedAt = status.UpdatedAtUtc.HasValue ? FormatDate(status.UpdatedAtUtc.Value) : "-";
    }

    private void ApplyNotificationPreference(NotificationPreferenceResponse preference)
    {
        NotificationEmailEnabled = preference.EmailEnabled;
        NotificationInAppEnabled = preference.InAppEnabled;
        NotificationSmsEnabled = preference.SmsEnabled;
    }

    private void ApplyCardControls(EnvelopePaymentCardControlResponse? control)
    {
        if (control is null)
        {
            CardControlDailyLimit = string.Empty;
            CardControlAllowedCategories = string.Empty;
            CardControlAllowedMerchants = string.Empty;
            CardControlSummary = "No controls configured.";
            return;
        }

        CardControlDailyLimit = control.DailyLimitAmount?.ToString("0.##") ?? string.Empty;
        CardControlAllowedCategories = string.Join(", ", control.AllowedMerchantCategories);
        CardControlAllowedMerchants = string.Join(", ", control.AllowedMerchantNames);
        CardControlSummary = $"Last updated {FormatDate(control.UpdatedAtUtc)}.";
    }

    private void ApplyCardControlAudit(IReadOnlyList<EnvelopePaymentCardControlAuditResponse> auditEntries)
    {
        CardControlAuditEntries = new ObservableCollection<CardControlAuditItemViewModel>(
            auditEntries
                .OrderByDescending(static entry => entry.ChangedAtUtc)
                .Select(static entry => new CardControlAuditItemViewModel(
                    entry.Id,
                    entry.Action,
                    entry.ChangedBy,
                    FormatDate(entry.ChangedAtUtc),
                    entry.PreviousStateJson ?? "{}",
                    entry.NewStateJson)));
    }

    private void ClearEnvelopeScopedState()
    {
        SelectedEnvelopeSummary = "No active envelope selected.";
        EnvelopeFinancialAccountSummary = "No linked financial account.";
        Cards = [];
        _isApplyingCardSelection = true;
        SelectedCard = null;
        _isApplyingCardSelection = false;
        OnPropertyChanged(nameof(HasCardSelection));
        OnPropertyChanged(nameof(SelectedCardIsPhysical));
        ClearSelectedCardState();
    }

    private void ClearSelectedCardState()
    {
        CardControlDailyLimit = string.Empty;
        CardControlAllowedCategories = string.Empty;
        CardControlAllowedMerchants = string.Empty;
        CardControlSummary = "No spending controls loaded.";
        CardControlAuditEntries = [];
        IssuanceSummary = "No physical card issuance selected.";
        CardSpendEvaluationResult = "No card spend evaluation has been run.";
    }

    private void SelectCard(Guid cardId)
    {
        var selected = Cards.FirstOrDefault(card => card.Id == cardId);
        _isApplyingCardSelection = true;
        SelectedCard = selected;
        _isApplyingCardSelection = false;
        OnPropertyChanged(nameof(HasCardSelection));
        OnPropertyChanged(nameof(SelectedCardIsPhysical));
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

    private static PaymentCardItemViewModel MapCard(EnvelopePaymentCardResponse card)
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
            FormatDate(card.CreatedAtUtc),
            FormatDate(card.UpdatedAtUtc));
    }

    private string GetMaskedIdentifierDisplay(string? value)
    {
        return ShowProviderIdentifiers
            ? NormalizeSensitiveValue(value)
            : SensitiveValueMasker.MaskIdentifier(value);
    }

    private static string NormalizeSensitiveValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }

    private static string FormatIssuanceSummary(EnvelopePhysicalCardIssuanceResponse issuance)
    {
        var tracking = string.IsNullOrWhiteSpace(issuance.Shipment.TrackingNumber) ? "n/a" : issuance.Shipment.TrackingNumber;
        var carrier = string.IsNullOrWhiteSpace(issuance.Shipment.Carrier) ? "pending carrier" : issuance.Shipment.Carrier;
        return
            $"Shipment {issuance.Shipment.Status} via {carrier}, tracking {tracking}, updated {FormatDate(issuance.Shipment.UpdatedAtUtc)}.";
    }

    private static string FormatStripeWebhookSimulationSummary(StripeWebhookProcessResponse result)
    {
        var eventType = string.IsNullOrWhiteSpace(result.EventType) ? "-" : result.EventType.Trim();
        var eventId = string.IsNullOrWhiteSpace(result.EventId) ? "-" : result.EventId.Trim();
        var message = string.IsNullOrWhiteSpace(result.Message) ? "-" : result.Message.Trim();
        return $"Outcome: {result.Outcome}. Event: {eventType}. EventId: {eventId}. Message: {message}";
    }

    private static string FormatDate(DateTimeOffset value)
    {
        return value.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    }
}
