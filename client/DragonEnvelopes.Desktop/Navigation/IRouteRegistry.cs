namespace DragonEnvelopes.Desktop.Navigation;

public interface IRouteRegistry
{
    IReadOnlyCollection<RouteDefinition> Routes { get; }

    bool TryGetRoute(string routeKey, out RouteDefinition route);
}
