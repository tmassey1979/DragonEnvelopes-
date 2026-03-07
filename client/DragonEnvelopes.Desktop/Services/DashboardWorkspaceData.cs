namespace DragonEnvelopes.Desktop.Services;

public sealed record DashboardWorkspaceData(
    int AccountCount,
    decimal NetWorth,
    decimal CashBalance,
    decimal MonthlySpend,
    decimal RemainingBudget,
    decimal BudgetHealthPercent,
    int GoalCount,
    int GoalsOnTrackCount,
    int GoalsBehindCount,
    IReadOnlyList<DashboardRecentTransactionData> RecentTransactions);

public sealed record DashboardRecentTransactionData(
    DateTimeOffset OccurredAt,
    string Merchant,
    decimal Amount,
    string Category,
    string EnvelopeName);
