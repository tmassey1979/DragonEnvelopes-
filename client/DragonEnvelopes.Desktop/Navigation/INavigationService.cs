using System.ComponentModel;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Navigation;

public interface INavigationService : INotifyPropertyChanged
{
    string CurrentRouteKey { get; }

    string CurrentTitle { get; }

    string CurrentSubtitle { get; }

    ShellContentViewModel? CurrentContent { get; }

    IReadOnlyCollection<RouteDefinition> Routes { get; }

    bool Navigate(string routeKey);
}
