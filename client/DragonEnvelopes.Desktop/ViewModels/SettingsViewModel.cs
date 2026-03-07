using System.Globalization;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Auth;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private static readonly string[] CurrencyCodes = ["USD", "CAD", "EUR", "GBP"];
    private static readonly string[] TimeZoneIds =
    [
        "America/Chicago",
        "America/New_York",
        "America/Denver",
        "America/Los_Angeles",
        "Europe/Berlin",
        "UTC"
    ];
    private static readonly string[] PayFrequencies = ["Weekly", "BiWeekly", "SemiMonthly", "Monthly"];
    private static readonly string[] BudgetingStyles = ["ZeroBased", "EnvelopePriority"];

    private readonly IAuthService _authService;
    private readonly ISystemStatusDataService _systemStatusDataService;
    private readonly IFamilySettingsDataService _familySettingsDataService;

    public SettingsViewModel(
        IAuthService authService,
        ISystemStatusDataService systemStatusDataService,
        IFamilySettingsDataService familySettingsDataService)
    {
        _authService = authService;
        _systemStatusDataService = systemStatusDataService;
        _familySettingsDataService = familySettingsDataService;
        SignOutCommand = new AsyncRelayCommand(SignOutAsync);
        ReloadStatusCommand = new AsyncRelayCommand(LoadStatusAsync);
        SaveFamilyProfileCommand = new AsyncRelayCommand(SaveFamilyProfileAsync);
        SaveBudgetPreferencesCommand = new AsyncRelayCommand(SaveBudgetPreferencesAsync);

        AppVersion = typeof(SettingsViewModel).Assembly.GetName().Version?.ToString() ?? "0.0.0";
        EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        SelectedCurrencyCode = CurrencyCodes[0];
        SelectedTimeZoneId = TimeZoneIds[0];
        SelectedPayFrequency = PayFrequencies[0];
        SelectedBudgetingStyle = BudgetingStyles[0];
        _ = LoadStatusAsync();
    }

    public IAsyncRelayCommand SignOutCommand { get; }

    public IAsyncRelayCommand ReloadStatusCommand { get; }

    public IAsyncRelayCommand SaveFamilyProfileCommand { get; }

    public IAsyncRelayCommand SaveBudgetPreferencesCommand { get; }

    public IReadOnlyList<string> CurrencyOptions { get; } = CurrencyCodes;

    public IReadOnlyList<string> TimeZoneOptions { get; } = TimeZoneIds;

    public IReadOnlyList<string> PayFrequencyOptions { get; } = PayFrequencies;

    public IReadOnlyList<string> BudgetingStyleOptions { get; } = BudgetingStyles;

    [ObservableProperty]
    private string appVersion = "0.0.0";

    [ObservableProperty]
    private string environmentName = "Development";

    [ObservableProperty]
    private bool hasActiveSession;

    [ObservableProperty]
    private string sessionStatus = "Checking session...";

    [ObservableProperty]
    private string backendHealthStatus = "Unknown";

    [ObservableProperty]
    private string backendVersion = "Unknown";

    [ObservableProperty]
    private string backendEnvironment = "Unknown";

    [ObservableProperty]
    private string backendCheckedAt = "Not checked";

    [ObservableProperty]
    private string backendStatusMessage = "Backend status not loaded.";

    [ObservableProperty]
    private string familyApiHealthStatus = "Unknown";

    [ObservableProperty]
    private string ledgerApiHealthStatus = "Unknown";

    [ObservableProperty]
    private string familyApiStatusMessage = "Family API status not loaded.";

    [ObservableProperty]
    private string ledgerApiStatusMessage = "Ledger API status not loaded.";

    [ObservableProperty]
    private bool hasSettingsError;

    [ObservableProperty]
    private string settingsErrorMessage = string.Empty;

    [ObservableProperty]
    private string settingsStatusMessage = "Family settings not loaded.";

    [ObservableProperty]
    private string familyNameDraft = string.Empty;

    [ObservableProperty]
    private string selectedCurrencyCode = CurrencyCodes[0];

    [ObservableProperty]
    private string selectedTimeZoneId = TimeZoneIds[0];

    [ObservableProperty]
    private string familyProfileSummary = "Family profile not loaded.";

    [ObservableProperty]
    private string selectedPayFrequency = PayFrequencies[0];

    [ObservableProperty]
    private string selectedBudgetingStyle = BudgetingStyles[0];

    [ObservableProperty]
    private string householdMonthlyIncomeDraft = string.Empty;

    [ObservableProperty]
    private string budgetPreferencesSummary = "Budget preferences not loaded.";

    [ObservableProperty]
    private ObservableCollection<CapabilityMatrixItemViewModel> capabilityMatrix = [];

    [ObservableProperty]
    private string capabilitySummary = "Capability matrix not loaded.";

    private async Task LoadStatusAsync()
    {
        ClearSettingsError();
        await LoadSessionStateAsync();
        await LoadBackendStatusAsync();
        await LoadFamilySettingsAsync();
        LoadCapabilityMatrix();
    }

    private async Task LoadSessionStateAsync()
    {
        var session = await _authService.TryRestoreSessionAsync();
        if (session is null)
        {
            HasActiveSession = false;
            SessionStatus = "No active desktop session";
            return;
        }

        HasActiveSession = true;
        SessionStatus = string.IsNullOrWhiteSpace(session.Subject)
            ? $"Session expires at {session.ExpiresAtUtc:yyyy-MM-dd HH:mm} UTC"
            : $"Session user: {session.Subject} (expires {session.ExpiresAtUtc:HH:mm} UTC)";
    }

    private async Task LoadBackendStatusAsync()
    {
        try
        {
            var status = await _systemStatusDataService.GetRuntimeStatusAsync();
            BackendHealthStatus = status.HealthStatus;
            BackendVersion = status.Version;
            BackendEnvironment = status.Environment;
            BackendCheckedAt = status.CheckedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
            BackendStatusMessage = "Backend reachable.";
            FamilyApiHealthStatus = status.FamilyApiHealthStatus;
            LedgerApiHealthStatus = status.LedgerApiHealthStatus;
            FamilyApiStatusMessage = status.FamilyApiStatusMessage;
            LedgerApiStatusMessage = status.LedgerApiStatusMessage;
        }
        catch (Exception ex)
        {
            BackendHealthStatus = "Unavailable";
            BackendStatusMessage = $"Backend status check failed: {ex.Message}";
            BackendCheckedAt = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
            FamilyApiHealthStatus = "Unavailable";
            LedgerApiHealthStatus = "Unavailable";
            FamilyApiStatusMessage = "Family API status unavailable because primary runtime status check failed.";
            LedgerApiStatusMessage = "Ledger API status unavailable because primary runtime status check failed.";
        }
    }

    private async Task LoadFamilySettingsAsync()
    {
        try
        {
            var profile = await _familySettingsDataService.GetFamilyProfileAsync();
            FamilyNameDraft = profile.Name;
            SelectedCurrencyCode = EnsureOption(profile.CurrencyCode, CurrencyCodes, CurrencyCodes[0]);
            SelectedTimeZoneId = EnsureOption(profile.TimeZoneId, TimeZoneIds, TimeZoneIds[0]);
            FamilyProfileSummary = $"Profile updated {profile.UpdatedAt.ToLocalTime():yyyy-MM-dd HH:mm}.";

            var budget = await _familySettingsDataService.GetBudgetPreferencesAsync();
            SelectedPayFrequency = EnsureOption(budget.PayFrequency, PayFrequencies, PayFrequencies[0]);
            SelectedBudgetingStyle = EnsureOption(budget.BudgetingStyle, BudgetingStyles, BudgetingStyles[0]);
            HouseholdMonthlyIncomeDraft = budget.HouseholdMonthlyIncome?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty;
            BudgetPreferencesSummary = $"Preferences updated {budget.UpdatedAt.ToLocalTime():yyyy-MM-dd HH:mm}.";
            SettingsStatusMessage = "Family settings loaded.";
        }
        catch (Exception ex)
        {
            SetSettingsError($"Family settings load failed: {ex.Message}");
        }
    }

    private async Task SaveFamilyProfileAsync()
    {
        ClearSettingsError();
        if (string.IsNullOrWhiteSpace(FamilyNameDraft))
        {
            SetSettingsError("Family name is required.");
            return;
        }

        if (!CurrencyCodes.Contains(SelectedCurrencyCode, StringComparer.OrdinalIgnoreCase))
        {
            SetSettingsError("Select a valid currency.");
            return;
        }

        if (!TimeZoneIds.Contains(SelectedTimeZoneId, StringComparer.Ordinal))
        {
            SetSettingsError("Select a valid time zone.");
            return;
        }

        try
        {
            var updated = await _familySettingsDataService.UpdateFamilyProfileAsync(
                FamilyNameDraft.Trim(),
                SelectedCurrencyCode.Trim().ToUpperInvariant(),
                SelectedTimeZoneId.Trim());

            FamilyNameDraft = updated.Name;
            SelectedCurrencyCode = EnsureOption(updated.CurrencyCode, CurrencyCodes, CurrencyCodes[0]);
            SelectedTimeZoneId = EnsureOption(updated.TimeZoneId, TimeZoneIds, TimeZoneIds[0]);
            FamilyProfileSummary = $"Profile saved at {updated.UpdatedAt.ToLocalTime():yyyy-MM-dd HH:mm}.";
            SettingsStatusMessage = "Family profile saved.";
        }
        catch (Exception ex)
        {
            SetSettingsError($"Family profile save failed: {ex.Message}");
        }
    }

    private async Task SaveBudgetPreferencesAsync()
    {
        ClearSettingsError();
        if (!PayFrequencies.Contains(SelectedPayFrequency, StringComparer.OrdinalIgnoreCase))
        {
            SetSettingsError("Select a valid pay frequency.");
            return;
        }

        if (!BudgetingStyles.Contains(SelectedBudgetingStyle, StringComparer.OrdinalIgnoreCase))
        {
            SetSettingsError("Select a valid budgeting style.");
            return;
        }

        decimal? householdIncome = null;
        if (!string.IsNullOrWhiteSpace(HouseholdMonthlyIncomeDraft))
        {
            if (!decimal.TryParse(
                    HouseholdMonthlyIncomeDraft,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var parsedIncome))
            {
                SetSettingsError("Household monthly income is invalid.");
                return;
            }

            if (parsedIncome < 0m)
            {
                SetSettingsError("Household monthly income cannot be negative.");
                return;
            }

            householdIncome = parsedIncome;
        }

        try
        {
            var updated = await _familySettingsDataService.UpdateBudgetPreferencesAsync(
                SelectedPayFrequency.Trim(),
                SelectedBudgetingStyle.Trim(),
                householdIncome);

            SelectedPayFrequency = EnsureOption(updated.PayFrequency, PayFrequencies, PayFrequencies[0]);
            SelectedBudgetingStyle = EnsureOption(updated.BudgetingStyle, BudgetingStyles, BudgetingStyles[0]);
            HouseholdMonthlyIncomeDraft = updated.HouseholdMonthlyIncome?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty;
            BudgetPreferencesSummary = $"Preferences saved at {updated.UpdatedAt.ToLocalTime():yyyy-MM-dd HH:mm}.";
            SettingsStatusMessage = "Budget preferences saved.";
        }
        catch (Exception ex)
        {
            SetSettingsError($"Budget preferences save failed: {ex.Message}");
        }
    }

    private async Task SignOutAsync()
    {
        await _authService.SignOutAsync();
        HasActiveSession = false;
        SessionStatus = "Session cleared. Sign in again from the shell header.";
    }

    private void ClearSettingsError()
    {
        HasSettingsError = false;
        SettingsErrorMessage = string.Empty;
    }

    private void SetSettingsError(string message)
    {
        HasSettingsError = true;
        SettingsErrorMessage = message;
        SettingsStatusMessage = message;
    }

    private void LoadCapabilityMatrix()
    {
        var items = BuildCapabilityManifest();
        CapabilityMatrix = new ObservableCollection<CapabilityMatrixItemViewModel>(items);

        var availableCount = items.Count(item => item.Status.Equals("Available", StringComparison.OrdinalIgnoreCase));
        var partialCount = items.Count(item => item.Status.Equals("Partial", StringComparison.OrdinalIgnoreCase));
        var notWiredCount = items.Count(item => item.Status.Equals("Not Wired", StringComparison.OrdinalIgnoreCase));
        CapabilitySummary = $"Capabilities: {availableCount} available, {partialCount} partial, {notWiredCount} not wired.";
    }

    private static IReadOnlyList<CapabilityMatrixItemViewModel> BuildCapabilityManifest()
    {
        return
        [
            new CapabilityMatrixItemViewModel("Family", "Family profile and budget preferences", "Available", "Settings profile and budget workflows are wired."),
            new CapabilityMatrixItemViewModel("Family", "Member add/list/update/remove", "Available", "Family workspace supports role update and member removal."),
            new CapabilityMatrixItemViewModel("Family", "Invites create/resend/cancel/redeem", "Available", "Family workspace + invite onboarding windows are wired."),
            new CapabilityMatrixItemViewModel("Family", "Invite event timeline", "Not Wired", "Lifecycle timeline API/UI not implemented yet."),

            new CapabilityMatrixItemViewModel("Ledger", "Accounts create/list", "Available", "Accounts workspace is fully wired."),
            new CapabilityMatrixItemViewModel("Ledger", "Transactions create/edit/delete/restore", "Available", "Transactions workspace supports active + deleted restore flows."),
            new CapabilityMatrixItemViewModel("Ledger", "Automation rule CRUD", "Available", "Automation workspace supports rule lifecycle and toggles."),
            new CapabilityMatrixItemViewModel("Ledger", "Recurring bills and projection", "Available", "Recurring workspace supports CRUD/projection/auto-post."),
            new CapabilityMatrixItemViewModel("Ledger", "CSV imports preview/commit", "Available", "Imports workspace supports preview + commit + status."),
            new CapabilityMatrixItemViewModel("Ledger", "Report queries", "Available", "Reports workspace is wired for current report endpoints."),

            new CapabilityMatrixItemViewModel("Financial", "Plaid link/sync/reconciliation", "Available", "Integrations workspace supports Plaid connect + refresh."),
            new CapabilityMatrixItemViewModel("Financial", "Stripe account and card lifecycle", "Available", "Integrations workspace supports setup, issuance, controls."),
            new CapabilityMatrixItemViewModel("Financial", "Provider timeline + notification replay", "Available", "Integrations workspace supports timeline and replay actions."),
            new CapabilityMatrixItemViewModel("Financial", "Webhook endpoint operations", "Partial", "Operational visibility is wired; direct webhook simulation UI is not."),

            new CapabilityMatrixItemViewModel("System", "Health/version/session diagnostics", "Available", "Settings includes health/version/session summaries."),
            new CapabilityMatrixItemViewModel("System", "Per-route role command gating matrix", "Partial", "Parent-only route gating exists; full command audit pending.")
        ];
    }

    private static string EnsureOption(string? value, IReadOnlyList<string> options, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var match = options.FirstOrDefault(option => option.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(match))
            {
                return match;
            }
        }

        return fallback;
    }
}
