using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Navigation;

public sealed class RouteRegistry : IRouteRegistry
{
    private readonly IReadOnlyDictionary<string, RouteDefinition> _routes;

    public RouteRegistry()
    {
        var routeList = new[]
        {
            new RouteDefinition(
                Key: "/dashboard",
                Label: "Dashboard",
                Glyph: "\uE80F",
                TopBarSubtitle: "Budget health and status",
                Content: new ShellContentViewModel(
                    "Budget Health Overview",
                    "Track allocation health, available cash, and month-to-date progress for your family.",
                    "Dashboard widgets will appear here",
                    "Connect real account and envelope data to populate KPI cards.",
                    metrics: BuildDashboardMetrics())),
            new RouteDefinition(
                Key: "/envelopes",
                Label: "Envelopes",
                Glyph: "\uE713",
                TopBarSubtitle: "Envelope planning workspace",
                Content: new ShellContentViewModel(
                    "Envelope Planning",
                    "Organize spending buckets and monthly targets across your household categories.",
                    "Envelope list is not loaded yet",
                    "Once envelope APIs are wired, this region will host list and edit views.",
                    metrics: BuildPendingMetrics("Envelope"),
                    isEmpty: true)),
            new RouteDefinition(
                Key: "/transactions",
                Label: "Transactions",
                Glyph: "\uE8A7",
                TopBarSubtitle: "Transaction feed and categorization",
                Content: new ShellContentViewModel(
                    "Transaction Activity",
                    "Review posted spending, categorize activity, and route expenses into envelopes.",
                    "Transaction feed is not connected",
                    "Upcoming tasks will bind this region to API-backed transaction pages.",
                    metrics: BuildPendingMetrics("Transaction"),
                    isLoading: true)),
            new RouteDefinition(
                Key: "/accounts",
                Label: "Accounts",
                Glyph: "\uEB0F",
                TopBarSubtitle: "Account balances and sources",
                Content: new ShellContentViewModel(
                    "Account Summary",
                    "Inspect account balances and connect transactions back to their source account.",
                    "Accounts are not loaded yet",
                    "Account APIs and bindings will hydrate this section in upcoming stories.",
                    metrics: BuildPendingMetrics("Account"),
                    isEmpty: true)),
            new RouteDefinition(
                Key: "/reports",
                Label: "Reports",
                Glyph: "\uE9D2",
                TopBarSubtitle: "Spend and budget reporting",
                Content: new ShellContentViewModel(
                    "Reporting Center",
                    "Analyze spending breakdowns, remaining budget, and monthly trend signals.",
                    "Report queries are not connected",
                    "When reporting handlers are available, visual summaries will render here.",
                    metrics: BuildPendingMetrics("Report"),
                    isLoading: true)),
            new RouteDefinition(
                Key: "/automation",
                Label: "Automation",
                Glyph: "\uE7B8",
                TopBarSubtitle: "Rules and allocation automations",
                Content: new ShellContentViewModel(
                    "Automation Rules",
                    "Define and tune categorization and allocation rules for incoming transactions.",
                    "Automation rules are not loaded yet",
                    "Rule management UI and APIs will fill this area in later tasks.",
                    metrics: BuildPendingMetrics("Rule"),
                    isEmpty: true)),
            new RouteDefinition(
                Key: "/settings",
                Label: "Settings",
                Glyph: "\uE713",
                TopBarSubtitle: "Session and profile settings",
                Content: new ShellContentViewModel(
                    "Application Settings",
                    "Manage profile/session preferences and local desktop configuration options.",
                    "Settings controls are not loaded yet",
                    "Future settings stories will add profile and session actions here.",
                    metrics: BuildPendingMetrics("Setting"),
                    isEmpty: true))
        };

        _routes = routeList.ToDictionary(static route => route.Key, StringComparer.OrdinalIgnoreCase);
        Routes = routeList;
    }

    public IReadOnlyCollection<RouteDefinition> Routes { get; }

    public bool TryGetRoute(string routeKey, out RouteDefinition route)
    {
        var normalizedKey = NormalizeRouteKey(routeKey);
        return _routes.TryGetValue(normalizedKey, out route!);
    }

    private static string NormalizeRouteKey(string routeKey)
    {
        if (string.IsNullOrWhiteSpace(routeKey))
        {
            return "/";
        }

        var trimmed = routeKey.Trim();
        return trimmed.StartsWith('/') ? trimmed : $"/{trimmed}";
    }

    private static IReadOnlyList<MetricTileViewModel> BuildDashboardMetrics()
    {
        return
        [
            new MetricTileViewModel("Remaining Budget", "$1,240", "+$210 vs last week", MetricTrendDirection.Positive),
            new MetricTileViewModel("Allocated", "72%", "On target", MetricTrendDirection.Neutral),
            new MetricTileViewModel("Weekly Spend", "$360", "-$45 vs average", MetricTrendDirection.Positive)
        ];
    }

    private static IReadOnlyList<MetricTileViewModel> BuildPendingMetrics(string subject)
    {
        return
        [
            new MetricTileViewModel($"{subject} Count", "--", "Awaiting API binding", MetricTrendDirection.Neutral, isLoading: true),
            new MetricTileViewModel($"{subject} Velocity", "--", "No activity yet", MetricTrendDirection.Neutral, isEmpty: true),
            new MetricTileViewModel($"{subject} Health", "--", "Pending integration", MetricTrendDirection.Neutral, isEmpty: true)
        ];
    }
}
