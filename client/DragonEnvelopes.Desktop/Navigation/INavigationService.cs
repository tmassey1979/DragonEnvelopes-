using System.ComponentModel;

namespace DragonEnvelopes.Desktop.Navigation;

public interface INavigationService : INotifyPropertyChanged
{
    string CurrentRouteKey { get; }

    string CurrentTitle { get; }

    string CurrentSubtitle { get; }

    object? CurrentContent { get; }

    IReadOnlyCollection<RouteDefinition> Routes { get; }

    bool Navigate(string routeKey);
}
