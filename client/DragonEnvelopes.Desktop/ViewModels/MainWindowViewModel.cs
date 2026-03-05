using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Auth;
using DragonEnvelopes.Desktop.Navigation;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IAuthService _authService;

    public MainWindowViewModel()
        : this(
            new NavigationService(new RouteRegistry()),
            new DesktopAuthService(new ProtectedTokenSessionStore()))
    {
    }

    public MainWindowViewModel(
        INavigationService navigationService,
        IAuthService authService)
    {
        _navigationService = navigationService;
        _authService = authService;

        NavigationItems = new ObservableCollection<NavigationItemViewModel>(
            navigationService.Routes.Select(static route =>
                new NavigationItemViewModel(route.Key, route.Label, route.Glyph, route.Content)));

        NavigateCommand = new RelayCommand<NavigationItemViewModel?>(Navigate);
        ToggleAuthenticationCommand = new AsyncRelayCommand(ToggleAuthenticationAsync);

        _navigationService.PropertyChanged += OnNavigationServiceChanged;
        _navigationService.Navigate("/dashboard");
        SyncFromNavigationService();
        _ = RestoreSessionAsync();
    }

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

    public IRelayCommand<NavigationItemViewModel?> NavigateCommand { get; }

    public IAsyncRelayCommand ToggleAuthenticationCommand { get; }

    [ObservableProperty]
    private string topBarTitle = "DragonEnvelopes";

    [ObservableProperty]
    private string topBarSubtitle = "Route not selected";

    [ObservableProperty]
    private ShellContentViewModel? currentContent;

    [ObservableProperty]
    private bool isAuthenticated;

    [ObservableProperty]
    private string authStatus = "Not signed in";

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
            await _authService.SignOutAsync();
            IsAuthenticated = false;
            AuthStatus = "Signed out";
            return;
        }

        AuthStatus = "Signing in...";
        var result = await _authService.SignInAsync();
        if (!result.Succeeded)
        {
            IsAuthenticated = false;
            AuthStatus = result.Message;
            return;
        }

        IsAuthenticated = true;
        AuthStatus = string.IsNullOrWhiteSpace(result.Session?.Subject)
            ? "Signed in"
            : $"Signed in as {result.Session.Subject}";
    }

    partial void OnIsAuthenticatedChanged(bool value)
    {
        OnPropertyChanged(nameof(AuthActionLabel));
    }
}
