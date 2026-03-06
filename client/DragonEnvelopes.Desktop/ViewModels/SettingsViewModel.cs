using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Auth;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ISystemStatusDataService _systemStatusDataService;

    public SettingsViewModel(IAuthService authService, ISystemStatusDataService systemStatusDataService)
    {
        _authService = authService;
        _systemStatusDataService = systemStatusDataService;
        SignOutCommand = new AsyncRelayCommand(SignOutAsync);
        ReloadStatusCommand = new AsyncRelayCommand(LoadStatusAsync);

        AppVersion = typeof(SettingsViewModel).Assembly.GetName().Version?.ToString() ?? "0.0.0";
        EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        _ = LoadStatusAsync();
    }

    public IAsyncRelayCommand SignOutCommand { get; }

    public IAsyncRelayCommand ReloadStatusCommand { get; }

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
    private string profilePlaceholderMessage =
        "Profile preferences are scaffolded and will be activated once profile APIs are implemented.";

    private async Task LoadStatusAsync()
    {
        await LoadSessionStateAsync();
        await LoadBackendStatusAsync();
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

    private async Task SignOutAsync()
    {
        await _authService.SignOutAsync();
        HasActiveSession = false;
        SessionStatus = "Session cleared. Sign in again from the shell header.";
    }
}
