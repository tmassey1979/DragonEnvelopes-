using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using DragonEnvelopes.Contracts.Families;
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
    private readonly IFamilySelectionStore _familySelectionStore;
    private bool _isApplyingFamilySelection;

    public MainWindowViewModel()
        : this(CreateDefaults())
    {
    }

    public MainWindowViewModel(
        INavigationService navigationService,
        IAuthService authService,
        IBackendApiClient apiClient,
        IFamilyContext familyContext,
        IFamilySelectionStore familySelectionStore)
    {
        _navigationService = navigationService;
        _authService = authService;
        _apiClient = apiClient;
        _familyContext = familyContext;
        _familySelectionStore = familySelectionStore;

        NavigationItems = new ObservableCollection<NavigationItemViewModel>(
            navigationService.Routes.Select(static route =>
                new NavigationItemViewModel(route.Key, route.Label, route.Glyph, route.Content, route.RequiredRole)));

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

    [ObservableProperty]
    private string roleSummary = "Role: unknown";

    [ObservableProperty]
    private bool isParentUser;

    [ObservableProperty]
    private ObservableCollection<FamilyOptionViewModel> availableFamilies = [];

    [ObservableProperty]
    private FamilyOptionViewModel? selectedFamily;

    public string AuthActionLabel => IsAuthenticated ? "Sign Out" : "Sign In";

    private void Navigate(NavigationItemViewModel? selectedItem)
    {
        if (selectedItem is null || !selectedItem.IsEnabled)
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
        AvailableFamilies.Clear();
        SelectedFamily = null;
        await _familySelectionStore.ClearAsync();
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
        IFamilyContext FamilyContext,
        IFamilySelectionStore FamilySelectionStore) CreateDefaults()
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
        var familySelectionStore = new ProtectedFamilySelectionStore();
        var navigationService = new NavigationService(new RouteRegistry(apiClient, authService, familyContext));
        return (navigationService, authService, apiClient, familyContext, familySelectionStore);
    }

    private MainWindowViewModel(
        (INavigationService NavigationService, IAuthService AuthService, IBackendApiClient ApiClient, IFamilyContext FamilyContext, IFamilySelectionStore FamilySelectionStore) defaults)
        : this(defaults.NavigationService, defaults.AuthService, defaults.ApiClient, defaults.FamilyContext, defaults.FamilySelectionStore)
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
                AvailableFamilies.Clear();
                SelectedFamily = null;
                RoleSummary = "Role: unknown";
                IsParentUser = false;
                return;
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(stream);
            var roles = document.RootElement.TryGetProperty("roles", out var rolesElement) && rolesElement.ValueKind == JsonValueKind.Array
                ? rolesElement.EnumerateArray().Select(static role => role.GetString())
                    .Where(static role => !string.IsNullOrWhiteSpace(role))
                    .ToArray()
                : [];
            IsParentUser = roles.Any(static role => string.Equals(role, "Parent", StringComparison.OrdinalIgnoreCase));
            RoleSummary = roles.Length == 0
                ? "Role: unknown"
                : $"Roles: {string.Join(", ", roles)}";
            ApplyRoleGates();

            if (!document.RootElement.TryGetProperty("familyIds", out var familyIdsElement) ||
                familyIdsElement.ValueKind != JsonValueKind.Array)
            {
                _familyContext.SetFamilyId(null);
                AvailableFamilies.Clear();
                SelectedFamily = null;
                return;
            }

            var familyIds = familyIdsElement.EnumerateArray()
                .Select(static element => element.GetString())
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => Guid.TryParse(value, out var parsed) ? parsed : Guid.Empty)
                .Where(static value => value != Guid.Empty)
                .Distinct()
                .ToArray();

            if (familyIds.Length == 0)
            {
                _familyContext.SetFamilyId(null);
                AvailableFamilies.Clear();
                SelectedFamily = null;
                await _familySelectionStore.ClearAsync();
                return;
            }

            var familyOptions = await LoadFamilyOptionsAsync(familyIds);
            AvailableFamilies = new ObservableCollection<FamilyOptionViewModel>(familyOptions);

            var persistedFamilyId = await _familySelectionStore.LoadAsync(cancellationToken: default);
            var preferredFamily = AvailableFamilies.FirstOrDefault(family =>
                    persistedFamilyId.HasValue && family.Id == persistedFamilyId.Value)
                ?? AvailableFamilies.FirstOrDefault();

            _isApplyingFamilySelection = true;
            SelectedFamily = preferredFamily;
            _isApplyingFamilySelection = false;
            _familyContext.SetFamilyId(preferredFamily?.Id);

            if (preferredFamily is not null)
            {
                await _familySelectionStore.SaveAsync(preferredFamily.Id);
            }

            await RefreshFamilyBoundViewModelsAsync();
        }
        catch
        {
            _familyContext.SetFamilyId(null);
            AvailableFamilies.Clear();
            SelectedFamily = null;
            RoleSummary = "Role: unknown";
            IsParentUser = false;
            ApplyRoleGates();
        }
    }

    partial void OnSelectedFamilyChanged(FamilyOptionViewModel? value)
    {
        if (_isApplyingFamilySelection)
        {
            return;
        }

        _ = ApplySelectedFamilyAsync(value);
    }

    private async Task ApplySelectedFamilyAsync(FamilyOptionViewModel? family)
    {
        _familyContext.SetFamilyId(family?.Id);
        if (family is null)
        {
            await _familySelectionStore.ClearAsync();
            return;
        }

        await _familySelectionStore.SaveAsync(family.Id);
        await RefreshFamilyBoundViewModelsAsync();
    }

    private async Task<IReadOnlyList<FamilyOptionViewModel>> LoadFamilyOptionsAsync(IEnumerable<Guid> familyIds)
    {
        var options = new List<FamilyOptionViewModel>();
        foreach (var familyId in familyIds)
        {
            try
            {
                using var response = await _apiClient.GetAsync($"families/{familyId}");
                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }

                await using var stream = await response.Content.ReadAsStreamAsync();
                var family = await JsonSerializer.DeserializeAsync<FamilyResponse>(stream);
                if (family is null)
                {
                    continue;
                }

                options.Add(new FamilyOptionViewModel(family.Id, family.Name));
            }
            catch
            {
                // Ignore individual family lookup failures and continue with remaining options.
            }
        }

        return options
            .OrderBy(static family => family.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task RefreshFamilyBoundViewModelsAsync()
    {
        foreach (var item in NavigationItems)
        {
            switch (item.Content)
            {
                case AccountsViewModel accounts:
                    await accounts.LoadCommand.ExecuteAsync(null);
                    break;
                case EnvelopesViewModel envelopes:
                    await envelopes.LoadCommand.ExecuteAsync(null);
                    break;
                case TransactionsViewModel transactions:
                    await transactions.LoadAccountsCommand.ExecuteAsync(null);
                    break;
                case BudgetsViewModel budgets:
                    await budgets.LoadCommand.ExecuteAsync(null);
                    break;
                case AutomationRulesViewModel automation:
                    await automation.LoadCommand.ExecuteAsync(null);
                    break;
            }
        }
    }

    private void ApplyRoleGates()
    {
        foreach (var item in NavigationItems)
        {
            item.IsEnabled = string.IsNullOrWhiteSpace(item.RequiredRole)
                || (string.Equals(item.RequiredRole, "Parent", StringComparison.OrdinalIgnoreCase) && IsParentUser);
        }

        if (NavigationItems.FirstOrDefault(item => item.IsSelected) is { IsEnabled: false })
        {
            _navigationService.Navigate("/dashboard");
            SyncFromNavigationService();
            ApiStatus = "Access adjusted: parent role required for selected route.";
        }
    }
}
