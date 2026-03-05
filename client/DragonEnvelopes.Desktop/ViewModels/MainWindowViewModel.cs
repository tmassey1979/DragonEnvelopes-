using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Navigation;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    public MainWindowViewModel()
        : this(new NavigationService(new RouteRegistry()))
    {
    }

    public MainWindowViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;

        NavigationItems = new ObservableCollection<NavigationItemViewModel>(
            navigationService.Routes.Select(static route =>
                new NavigationItemViewModel(route.Key, route.Label, route.Glyph, route.Content)));

        NavigateCommand = new RelayCommand<NavigationItemViewModel?>(Navigate);

        _navigationService.PropertyChanged += OnNavigationServiceChanged;
        _navigationService.Navigate("/dashboard");
        SyncFromNavigationService();
    }

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

    public IRelayCommand<NavigationItemViewModel?> NavigateCommand { get; }

    [ObservableProperty]
    private string topBarTitle = "DragonEnvelopes";

    [ObservableProperty]
    private string topBarSubtitle = "Route not selected";

    [ObservableProperty]
    private ShellContentViewModel? currentContent;

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
}
