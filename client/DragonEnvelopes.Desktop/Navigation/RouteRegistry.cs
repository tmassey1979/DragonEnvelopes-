using DragonEnvelopes.Desktop.ViewModels;
using CommunityToolkit.Mvvm.Input;
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
                    transactions: BuildTransactionRows())),
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

    private static IReadOnlyList<EnvelopeTileViewModel> BuildEnvelopeTiles()
    {
        return
        [
            new EnvelopeTileViewModel("Groceries", "$520", "$188", isSelected: true),
            new EnvelopeTileViewModel("Utilities", "$310", "$122"),
            new EnvelopeTileViewModel("School", "$240", "$196"),
            new EnvelopeTileViewModel("Fuel", "$180", "$64")
        ];
    }

    private static IReadOnlyList<TransactionRowViewModel> BuildTransactionRows()
    {
        var editCommand = new RelayCommand(static () => { });
        var splitCommand = new RelayCommand(static () => { });

        return
        [
            new TransactionRowViewModel("2026-03-03", "Whole Foods", "$82.49", "Groceries", "Food", isSelected: true, editCommand: editCommand, splitCommand: splitCommand),
            new TransactionRowViewModel("2026-03-02", "City Utilities", "$124.10", "Utilities", "Bills", isEdited: true, editCommand: editCommand, splitCommand: splitCommand),
            new TransactionRowViewModel("2026-03-01", "Fuel Stop", "$47.32", "Fuel", "Transport", isFlagged: true, editCommand: editCommand, splitCommand: splitCommand),
            new TransactionRowViewModel("2026-02-28", "School Lunch", "$19.75", "School", "Kids", editCommand: editCommand, splitCommand: splitCommand)
        ];
    }
}
