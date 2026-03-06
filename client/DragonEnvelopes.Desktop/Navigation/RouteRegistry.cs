using DragonEnvelopes.Desktop.ViewModels;
using DragonEnvelopes.Desktop.Api;
using DragonEnvelopes.Desktop.Auth;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.Navigation;

public sealed class RouteRegistry : IRouteRegistry
{
    private readonly IReadOnlyDictionary<string, RouteDefinition> _routes;

    public RouteRegistry(IBackendApiClient apiClient, IAuthService authService, IFamilyContext familyContext)
    {
        var routeList = new[]
        {
            new RouteDefinition(
                Key: "/dashboard",
                Label: "Dashboard",
                Glyph: "\uE80F",
                TopBarSubtitle: "Budget health and status",
                Content: new DashboardViewModel()),
            new RouteDefinition(
                Key: "/envelopes",
                Label: "Envelopes",
                Glyph: "\uE713",
                TopBarSubtitle: "Envelope planning workspace",
                Content: new EnvelopesViewModel(new EnvelopesDataService(apiClient, familyContext))),
            new RouteDefinition(
                Key: "/budgets",
                Label: "Budgets",
                Glyph: "\uE9D2",
                TopBarSubtitle: "Budget allocation and coverage",
                Content: new BudgetsViewModel(new BudgetsDataService(apiClient, familyContext))),
            new RouteDefinition(
                Key: "/transactions",
                Label: "Transactions",
                Glyph: "\uE8A7",
                TopBarSubtitle: "Transaction feed and categorization",
                Content: new TransactionsViewModel(new TransactionsDataService(apiClient, familyContext))),
            new RouteDefinition(
                Key: "/accounts",
                Label: "Accounts",
                Glyph: "\uEB0F",
                TopBarSubtitle: "Account balances and sources",
                Content: new AccountsViewModel(new AccountsDataService(apiClient, familyContext))),
            new RouteDefinition(
                Key: "/reports",
                Label: "Reports",
                Glyph: "\uE9D2",
                TopBarSubtitle: "Spend and budget reporting",
                Content: new ReportsViewModel(new ReportsDataService(apiClient))),
            new RouteDefinition(
                Key: "/automation",
                Label: "Automation",
                Glyph: "\uE7B8",
                TopBarSubtitle: "Rules and allocation automations",
                Content: new AutomationRulesViewModel(new AutomationRulesDataService(apiClient, familyContext))),
            new RouteDefinition(
                Key: "/family-members",
                Label: "Family",
                Glyph: "\uE716",
                TopBarSubtitle: "Household membership management",
                Content: new FamilyMembersViewModel(new FamilyMembersDataService(apiClient, familyContext))),
            new RouteDefinition(
                Key: "/recurring-bills",
                Label: "Recurring Bills",
                Glyph: "\uE823",
                TopBarSubtitle: "Recurring bills and upcoming projection",
                Content: new RecurringBillsViewModel(new RecurringBillsDataService(apiClient, familyContext))),
            new RouteDefinition(
                Key: "/settings",
                Label: "Settings",
                Glyph: "\uE713",
                TopBarSubtitle: "Session and profile settings",
                Content: new SettingsViewModel(authService))
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
