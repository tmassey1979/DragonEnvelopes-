using CommunityToolkit.Mvvm.ComponentModel;

namespace DragonEnvelopes.Desktop.Navigation;

public sealed partial class NavigationService : ObservableObject, INavigationService
{
    private readonly IRouteRegistry _routeRegistry;

    public NavigationService(IRouteRegistry routeRegistry)
    {
        _routeRegistry = routeRegistry;
        Routes = routeRegistry.Routes;
    }

    public IReadOnlyCollection<RouteDefinition> Routes { get; }

    [ObservableProperty]
    private string currentRouteKey = "/dashboard";

    [ObservableProperty]
    private string currentTitle = "Dashboard";

    [ObservableProperty]
    private string currentSubtitle = "Budget health and status";

    [ObservableProperty]
    private object? currentContent;

    public bool Navigate(string routeKey)
    {
        if (!_routeRegistry.TryGetRoute(routeKey, out var route))
        {
            return false;
        }

        CurrentRouteKey = route.Key;
        CurrentTitle = route.Label;
        CurrentSubtitle = route.TopBarSubtitle;
        CurrentContent = route.Content;
        return true;
    }
}
