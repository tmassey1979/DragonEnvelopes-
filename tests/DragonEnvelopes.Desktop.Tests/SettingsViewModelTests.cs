using DragonEnvelopes.Desktop.Auth;
using DragonEnvelopes.Desktop.Services;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Tests;

public sealed class SettingsViewModelTests
{
    [Fact]
    public async Task ReloadStatusCommand_LoadsFamilyProfileAndBudgetPreferences()
    {
        var authService = new FakeAuthService();
        var runtimeStatusService = new FakeSystemStatusDataService();
        var settingsDataService = new FakeFamilySettingsDataService();
        var viewModel = new SettingsViewModel(authService, runtimeStatusService, settingsDataService);

        await viewModel.ReloadStatusCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasSettingsError);
        Assert.Equal("Dragon Family", viewModel.FamilyNameDraft);
        Assert.Equal("USD", viewModel.SelectedCurrencyCode);
        Assert.Equal("America/Chicago", viewModel.SelectedTimeZoneId);
        Assert.Equal("BiWeekly", viewModel.SelectedPayFrequency);
        Assert.Equal("ZeroBased", viewModel.SelectedBudgetingStyle);
        Assert.Equal("6200", viewModel.HouseholdMonthlyIncomeDraft);
        Assert.Equal("Healthy", viewModel.BackendHealthStatus);
        Assert.Equal("Healthy", viewModel.FamilyApiHealthStatus);
        Assert.Equal("Unavailable", viewModel.LedgerApiHealthStatus);
        Assert.Equal("Healthy", viewModel.FinancialApiHealthStatus);
        Assert.Contains("reachable", viewModel.FamilyApiStatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("timed out", viewModel.LedgerApiStatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reachable", viewModel.FinancialApiStatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("settings-user", viewModel.SessionStatus, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(viewModel.CapabilityMatrix);
        Assert.Contains(viewModel.CapabilityMatrix, item => item.Status == "Available");
        Assert.Contains("available", viewModel.CapabilitySummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReloadStatusCommand_PopulatesCapabilityMatrixWithExpectedDomains()
    {
        var viewModel = new SettingsViewModel(
            new FakeAuthService(),
            new FakeSystemStatusDataService(),
            new FakeFamilySettingsDataService());

        await viewModel.ReloadStatusCommand.ExecuteAsync(null);

        Assert.Contains(viewModel.CapabilityMatrix, item => item.Domain == "Family");
        Assert.Contains(viewModel.CapabilityMatrix, item => item.Domain == "Ledger");
        Assert.Contains(viewModel.CapabilityMatrix, item => item.Domain == "Financial");
        Assert.Contains(viewModel.CapabilityMatrix, item => item.Domain == "System");
    }

    [Fact]
    public async Task SaveFamilyProfileCommand_UpdatesProfileAndSummary()
    {
        var settingsDataService = new FakeFamilySettingsDataService();
        var viewModel = new SettingsViewModel(
            new FakeAuthService(),
            new FakeSystemStatusDataService(),
            settingsDataService);

        await viewModel.ReloadStatusCommand.ExecuteAsync(null);
        viewModel.FamilyNameDraft = "Dragon Household";
        viewModel.SelectedCurrencyCode = "EUR";
        viewModel.SelectedTimeZoneId = "Europe/Berlin";

        await viewModel.SaveFamilyProfileCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasSettingsError);
        Assert.Equal(1, settingsDataService.UpdateFamilyProfileCallCount);
        Assert.Equal("Dragon Household", settingsDataService.LastProfileName);
        Assert.Equal("EUR", settingsDataService.LastProfileCurrencyCode);
        Assert.Equal("Europe/Berlin", settingsDataService.LastProfileTimeZoneId);
        Assert.Equal("Family profile saved.", viewModel.SettingsStatusMessage);
        Assert.Contains("Profile saved at", viewModel.FamilyProfileSummary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveBudgetPreferencesCommand_UpdatesPreferencesAndSummary()
    {
        var settingsDataService = new FakeFamilySettingsDataService();
        var viewModel = new SettingsViewModel(
            new FakeAuthService(),
            new FakeSystemStatusDataService(),
            settingsDataService);

        await viewModel.ReloadStatusCommand.ExecuteAsync(null);
        viewModel.SelectedPayFrequency = "Monthly";
        viewModel.SelectedBudgetingStyle = "EnvelopePriority";
        viewModel.HouseholdMonthlyIncomeDraft = "7100.50";

        await viewModel.SaveBudgetPreferencesCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasSettingsError);
        Assert.Equal(1, settingsDataService.UpdateBudgetPreferencesCallCount);
        Assert.Equal("Monthly", settingsDataService.LastPayFrequency);
        Assert.Equal("EnvelopePriority", settingsDataService.LastBudgetingStyle);
        Assert.Equal(7100.50m, settingsDataService.LastHouseholdMonthlyIncome);
        Assert.Equal("Budget preferences saved.", viewModel.SettingsStatusMessage);
        Assert.Contains("Preferences saved at", viewModel.BudgetPreferencesSummary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveBudgetPreferencesCommand_InvalidIncome_SetsValidationError()
    {
        var settingsDataService = new FakeFamilySettingsDataService();
        var viewModel = new SettingsViewModel(
            new FakeAuthService(),
            new FakeSystemStatusDataService(),
            settingsDataService);

        await viewModel.ReloadStatusCommand.ExecuteAsync(null);
        viewModel.HouseholdMonthlyIncomeDraft = "not-a-number";

        await viewModel.SaveBudgetPreferencesCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasSettingsError);
        Assert.Equal("Household monthly income is invalid.", viewModel.SettingsErrorMessage);
        Assert.Equal(0, settingsDataService.UpdateBudgetPreferencesCallCount);
    }

    [Fact]
    public async Task RefreshSessionNowCommand_RefreshesSessionAndAddsDiagnostic()
    {
        var authService = new FakeAuthService();
        var viewModel = new SettingsViewModel(
            authService,
            new FakeSystemStatusDataService(),
            new FakeFamilySettingsDataService());

        await viewModel.RefreshSessionNowCommand.ExecuteAsync(null);

        Assert.Equal(1, authService.GetAccessTokenCallCount);
        Assert.True(authService.LastForceRefresh);
        Assert.Contains(viewModel.AuthDiagnostics, item => item.Level == "Info");
        Assert.Contains("refreshed", viewModel.AuthRecoveryMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ClearSessionCommand_ClearsSessionAndAddsDiagnostic()
    {
        var authService = new FakeAuthService();
        var viewModel = new SettingsViewModel(
            authService,
            new FakeSystemStatusDataService(),
            new FakeFamilySettingsDataService());

        await viewModel.ClearSessionCommand.ExecuteAsync(null);

        Assert.Equal(1, authService.SignOutCallCount);
        Assert.False(viewModel.HasActiveSession);
        Assert.Equal("Not signed in", viewModel.SessionUser);
        Assert.Contains(viewModel.AuthDiagnostics, item => item.Message.Contains("cleared", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ReauthenticateGuidanceCommand_SetsGuidanceMessage()
    {
        var authService = new FakeAuthService();
        var viewModel = new SettingsViewModel(
            authService,
            new FakeSystemStatusDataService(),
            new FakeFamilySettingsDataService());

        await viewModel.ReauthenticateGuidanceCommand.ExecuteAsync(null);

        Assert.Equal(1, authService.SignOutCallCount);
        Assert.Contains("Sign In", viewModel.AuthRecoveryMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(viewModel.AuthDiagnostics, item => item.Level == "Warning");
    }

    private sealed class FakeAuthService : IAuthService
    {
        private AuthSession? _session = new()
        {
            AccessToken = "token",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1),
            Subject = "settings-user",
            RefreshToken = "refresh-token"
        };

        public int SignOutCallCount { get; private set; }

        public int GetAccessTokenCallCount { get; private set; }

        public bool LastForceRefresh { get; private set; }

        public Task<AuthSession?> TryRestoreSessionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_session);
        }

        public Task<AuthSignInResult> SignInAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AuthSignInResult(false, false, "not used"));
        }

        public Task<AuthSignInResult> SignInWithPasswordAsync(
            string usernameOrEmail,
            string password,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AuthSignInResult(false, false, "not used"));
        }

        public Task SignOutAsync(CancellationToken cancellationToken = default)
        {
            SignOutCallCount += 1;
            _session = null;
            return Task.CompletedTask;
        }

        public Task<string?> GetAccessTokenAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            GetAccessTokenCallCount += 1;
            LastForceRefresh = forceRefresh;
            return Task.FromResult<string?>(_session?.AccessToken);
        }
    }

    private sealed class FakeSystemStatusDataService : ISystemStatusDataService
    {
        public Task<SystemRuntimeStatusData> GetRuntimeStatusAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SystemRuntimeStatusData(
                "Healthy",
                "1.2.3",
                "Testing",
                DateTimeOffset.UtcNow,
                "Healthy",
                "Unavailable",
                "Family API reachable at http://localhost:18089/health/ready.",
                "Ledger API timed out at http://localhost:18090/health/ready.",
                "Healthy",
                "Financial API reachable at http://localhost:18091/health/ready."));
        }
    }

    private sealed class FakeFamilySettingsDataService : IFamilySettingsDataService
    {
        public int UpdateFamilyProfileCallCount { get; private set; }

        public int UpdateBudgetPreferencesCallCount { get; private set; }

        public string LastProfileName { get; private set; } = string.Empty;

        public string LastProfileCurrencyCode { get; private set; } = string.Empty;

        public string LastProfileTimeZoneId { get; private set; } = string.Empty;

        public string LastPayFrequency { get; private set; } = string.Empty;

        public string LastBudgetingStyle { get; private set; } = string.Empty;

        public decimal? LastHouseholdMonthlyIncome { get; private set; }

        public Task<FamilyProfileData> GetFamilyProfileAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new FamilyProfileData(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                "Dragon Family",
                "USD",
                "America/Chicago",
                DateTimeOffset.UtcNow.AddDays(-10),
                DateTimeOffset.UtcNow.AddDays(-1)));
        }

        public Task<FamilyProfileData> UpdateFamilyProfileAsync(
            string name,
            string currencyCode,
            string timeZoneId,
            CancellationToken cancellationToken = default)
        {
            UpdateFamilyProfileCallCount += 1;
            LastProfileName = name;
            LastProfileCurrencyCode = currencyCode;
            LastProfileTimeZoneId = timeZoneId;

            return Task.FromResult(new FamilyProfileData(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                name,
                currencyCode,
                timeZoneId,
                DateTimeOffset.UtcNow.AddDays(-10),
                DateTimeOffset.UtcNow));
        }

        public Task<FamilyBudgetPreferencesData> GetBudgetPreferencesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new FamilyBudgetPreferencesData(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                "BiWeekly",
                "ZeroBased",
                6200m,
                DateTimeOffset.UtcNow.AddDays(-2)));
        }

        public Task<FamilyBudgetPreferencesData> UpdateBudgetPreferencesAsync(
            string payFrequency,
            string budgetingStyle,
            decimal? householdMonthlyIncome,
            CancellationToken cancellationToken = default)
        {
            UpdateBudgetPreferencesCallCount += 1;
            LastPayFrequency = payFrequency;
            LastBudgetingStyle = budgetingStyle;
            LastHouseholdMonthlyIncome = householdMonthlyIncome;

            return Task.FromResult(new FamilyBudgetPreferencesData(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                payFrequency,
                budgetingStyle,
                householdMonthlyIncome,
                DateTimeOffset.UtcNow));
        }
    }
}
