using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Api;
using DragonEnvelopes.Desktop.Auth;
using DragonEnvelopes.Desktop.Navigation;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IAuthService _authService;
    private readonly IBackendApiClient _apiClient;

    public MainWindowViewModel()
        : this(CreateDefaults())
    {
    }

    public MainWindowViewModel(
        INavigationService navigationService,
        IAuthService authService,
        IBackendApiClient apiClient)
    {
        _navigationService = navigationService;
        _authService = authService;
        _apiClient = apiClient;

        NavigationItems = new ObservableCollection<NavigationItemViewModel>(
            navigationService.Routes.Select(static route =>
                new NavigationItemViewModel(route.Key, route.Label, route.Glyph, route.Content)));

        NavigateCommand = new RelayCommand<NavigationItemViewModel?>(Navigate);
        ToggleAuthenticationCommand = new AsyncRelayCommand(ToggleAuthenticationAsync);
        PingApiCommand = new AsyncRelayCommand(PingApiAsync);

        _navigationService.PropertyChanged += OnNavigationServiceChanged;
        _navigationService.Navigate("/dashboard");
        SyncFromNavigationService();
        _ = RestoreSessionAsync();
    }

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

    public IRelayCommand<NavigationItemViewModel?> NavigateCommand { get; }

    public IAsyncRelayCommand ToggleAuthenticationCommand { get; }

    public IAsyncRelayCommand PingApiCommand { get; }

    [ObservableProperty]
    private string topBarTitle = "DragonEnvelopes";

    [ObservableProperty]
    private string topBarSubtitle = "Route not selected";

    [ObservableProperty]
    private object? currentContent;

    [ObservableProperty]
    private bool isAuthenticated;

    [ObservableProperty]
    private string authStatus = "Not signed in";

    [ObservableProperty]
    private string apiStatus = "API idle";

    public string AuthActionLabel => IsAuthenticated ? "Sign Out" : "Sign In";

    private void Navigate(NavigationItemViewModel? selectedItem)
    {
        if (selectedItem is null)
        {
            return;
        }

        _navigationService.Navigate(selectedItem.Key);
    }

    private void OnNavigationServiceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(INavigationService.CurrentRouteKey)
            or nameof(INavigationService.CurrentTitle)
            or nameof(INavigationService.CurrentSubtitle)
            or nameof(INavigationService.CurrentContent))
        {
            SyncFromNavigationService();
        }
    }

    private void SyncFromNavigationService()
    {
        TopBarTitle = _navigationService.CurrentTitle;
        TopBarSubtitle = _navigationService.CurrentSubtitle;
        CurrentContent = _navigationService.CurrentContent;

        foreach (var item in NavigationItems)
        {
            item.IsSelected = string.Equals(
                item.Key,
                _navigationService.CurrentRouteKey,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    private async Task RestoreSessionAsync()
    {
        var session = await _authService.TryRestoreSessionAsync();
        if (session is null)
        {
            IsAuthenticated = false;
            AuthStatus = "Not signed in";
            return;
        }

        IsAuthenticated = true;
        AuthStatus = string.IsNullOrWhiteSpace(session.Subject)
            ? "Signed in"
            : $"Signed in as {session.Subject}";
    }

    private async Task ToggleAuthenticationAsync()
    {
        if (IsAuthenticated)
        {
            await SignOutAsync();
            return;
        }

        AuthStatus = "Use in-app login.";
    }

    public async Task SignOutAsync()
    {
        await _authService.SignOutAsync();
        IsAuthenticated = false;
        AuthStatus = "Signed out";
    }

    public async Task<AuthSignInResult> SignInWithPasswordAsync(string usernameOrEmail, string password)
    {
        AuthStatus = "Signing in...";
        var result = await _authService.SignInWithPasswordAsync(usernameOrEmail, password);
        if (!result.Succeeded)
        {
            IsAuthenticated = false;
            AuthStatus = result.Message;
            return result;
        }

        IsAuthenticated = true;
        AuthStatus = string.IsNullOrWhiteSpace(result.Session?.Subject)
            ? "Signed in"
            : $"Signed in as {result.Session.Subject}";

        return result;
    }

    private async Task PingApiAsync()
    {
        try
        {
            using var response = await _apiClient.GetAsync("auth/me");
            ApiStatus = $"API {(int)response.StatusCode}";

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                IsAuthenticated = false;
                AuthStatus = "Session invalid. Sign in again.";
            }
        }
        catch (Exception ex)
        {
            ApiStatus = $"API error: {ex.Message}";
        }
    }

    partial void OnIsAuthenticatedChanged(bool value)
    {
        OnPropertyChanged(nameof(AuthActionLabel));
    }

    private static (INavigationService NavigationService, IAuthService AuthService, IBackendApiClient ApiClient) CreateDefaults()
    {
        var authService = new DesktopAuthService(new ProtectedTokenSessionStore());
        var apiOptions = new ApiClientOptions();
        var handler = new AuthenticatedApiHttpMessageHandler(authService)
        {
            InnerHandler = new HttpClientHandler()
        };

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(apiOptions.BaseUrl, UriKind.Absolute)
        };

        var apiClient = new DragonEnvelopesApiClient(httpClient);
        var navigationService = new NavigationService(new RouteRegistry(apiClient, authService));
        return (navigationService, authService, apiClient);
    }

    private MainWindowViewModel(
        (INavigationService NavigationService, IAuthService AuthService, IBackendApiClient ApiClient) defaults)
        : this(defaults.NavigationService, defaults.AuthService, defaults.ApiClient)
    {
    }
}
