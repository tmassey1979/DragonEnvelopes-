using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Auth;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    public SettingsViewModel(IAuthService authService)
    {
        _authService = authService;
        SignOutCommand = new AsyncRelayCommand(SignOutAsync);
        ReloadSessionCommand = new AsyncRelayCommand(LoadSessionStateAsync);

        AppVersion = typeof(SettingsViewModel).Assembly.GetName().Version?.ToString() ?? "0.0.0";
        EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        _ = LoadSessionStateAsync();
    }

    public IAsyncRelayCommand SignOutCommand { get; }

    public IAsyncRelayCommand ReloadSessionCommand { get; }

    [ObservableProperty]
    private string appVersion = "0.0.0";

    [ObservableProperty]
    private string environmentName = "Development";

    [ObservableProperty]
    private bool hasActiveSession;

    [ObservableProperty]
    private string sessionStatus = "Checking session...";

    [ObservableProperty]
    private string profilePlaceholderMessage =
        "Profile preferences are scaffolded and will be activated once profile APIs are implemented.";

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

    private async Task SignOutAsync()
    {
        await _authService.SignOutAsync();
        HasActiveSession = false;
        SessionStatus = "Session cleared. Sign in again from the shell header.";
    }
}
