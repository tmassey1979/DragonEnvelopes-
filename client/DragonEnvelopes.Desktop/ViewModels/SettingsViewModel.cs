using System.Globalization;
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

    private async Task LoadStatusAsync()
    {
        ClearSettingsError();
        await LoadSessionStateAsync();
        await LoadBackendStatusAsync();
        await LoadFamilySettingsAsync();
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
        }
        catch (Exception ex)
        {
            BackendHealthStatus = "Unavailable";
            BackendStatusMessage = $"Backend status check failed: {ex.Message}";
            BackendCheckedAt = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
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
