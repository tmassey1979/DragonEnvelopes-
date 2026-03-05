using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Api;
using DragonEnvelopes.Desktop.Auth;
using DragonEnvelopes.Desktop.Navigation;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IAuthService _authService;
    private readonly IBackendApiClient _apiClient;
    private readonly IFamilyContext _familyContext;

    public MainWindowViewModel()
        : this(CreateDefaults())
    {
    }

    public MainWindowViewModel(
        INavigationService navigationService,
        IAuthService authService,
        IBackendApiClient apiClient,
        IFamilyContext familyContext)
    {
        _navigationService = navigationService;
        _authService = authService;
        _apiClient = apiClient;
        _familyContext = familyContext;

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
            _familyContext.SetFamilyId(null);
            return;
        }

        IsAuthenticated = true;
        AuthStatus = string.IsNullOrWhiteSpace(session.Subject)
            ? "Signed in"
            : $"Signed in as {session.Subject}";
        await RefreshFamilyContextAsync();
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
        _familyContext.SetFamilyId(null);
    }

    public async Task<AuthSignInResult> SignInWithPasswordAsync(string usernameOrEmail, string password)
    {
        AuthStatus = "Signing in...";
        var result = await _authService.SignInWithPasswordAsync(usernameOrEmail, password);
        if (!result.Succeeded)
        {
            IsAuthenticated = false;
            AuthStatus = result.Message;
            _familyContext.SetFamilyId(null);
            return result;
        }

        IsAuthenticated = true;
        AuthStatus = string.IsNullOrWhiteSpace(result.Session?.Subject)
            ? "Signed in"
            : $"Signed in as {result.Session.Subject}";
        await RefreshFamilyContextAsync();

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

    private static (
        INavigationService NavigationService,
        IAuthService AuthService,
        IBackendApiClient ApiClient,
        IFamilyContext FamilyContext) CreateDefaults()
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
        var familyContext = new FamilyContext();
        var navigationService = new NavigationService(new RouteRegistry(apiClient, authService, familyContext));
        return (navigationService, authService, apiClient, familyContext);
    }

    private MainWindowViewModel(
        (INavigationService NavigationService, IAuthService AuthService, IBackendApiClient ApiClient, IFamilyContext FamilyContext) defaults)
        : this(defaults.NavigationService, defaults.AuthService, defaults.ApiClient, defaults.FamilyContext)
    {
    }

    private async Task RefreshFamilyContextAsync()
    {
        try
        {
            using var response = await _apiClient.GetAsync("auth/me");
            if (!response.IsSuccessStatusCode)
            {
                _familyContext.SetFamilyId(null);
                return;
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(stream);
            if (!document.RootElement.TryGetProperty("familyIds", out var familyIdsElement) ||
                familyIdsElement.ValueKind != JsonValueKind.Array)
            {
                _familyContext.SetFamilyId(null);
                return;
            }

            var familyId = familyIdsElement.EnumerateArray()
                .Select(static element => element.GetString())
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => Guid.TryParse(value, out var parsed) ? parsed : Guid.Empty)
                .FirstOrDefault(static value => value != Guid.Empty);

            _familyContext.SetFamilyId(familyId == Guid.Empty ? null : familyId);
        }
        catch
        {
            _familyContext.SetFamilyId(null);
        }
    }
}
