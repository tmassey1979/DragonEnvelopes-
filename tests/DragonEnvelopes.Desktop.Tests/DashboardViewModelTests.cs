using DragonEnvelopes.Desktop.Services;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Tests;

public sealed class DashboardViewModelTests
{
    [Fact]
    public async Task LoadCommand_PopulatesKpisAndRecentTransactions()
    {
        var service = new FakeDashboardDataService
        {
            Workspace = new DashboardWorkspaceData(
                AccountCount: 2,
                NetWorth: 14250.40m,
                CashBalance: 5310.12m,
                MonthlySpend: 1288.50m,
                RemainingBudget: 2111.50m,
                BudgetHealthPercent: 62.1m,
                GoalCount: 3,
                GoalsOnTrackCount: 2,
                GoalsBehindCount: 1,
                RecentTransactions:
                [
                    new DashboardRecentTransactionData(
                        DateTimeOffset.UtcNow.AddDays(-1),
                        "Trader Joe's",
                        -64.31m,
                        "Food",
                        "Groceries"),
                    new DashboardRecentTransactionData(
                        DateTimeOffset.UtcNow.AddDays(-2),
                        "ComEd",
                        -118.07m,
                        "Bills",
                        "Utilities")
                ])
        };
        var viewModel = new DashboardViewModel(service, autoLoad: false);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasError);
        Assert.False(viewModel.IsEmpty);
        Assert.False(viewModel.IsKpiEmpty);
        Assert.False(viewModel.IsRecentTransactionsEmpty);
        Assert.Equal(5, viewModel.KpiCards.Count);
        Assert.Equal(2, viewModel.RecentTransactions.Count);
        Assert.Equal("Trader Joe's", viewModel.RecentTransactions[0].Merchant);
    }

    [Fact]
    public async Task LoadCommand_EmptyWorkspace_SetsIsEmptyTrue()
    {
        var service = new FakeDashboardDataService
        {
            Workspace = new DashboardWorkspaceData(
                AccountCount: 0,
                NetWorth: 0m,
                CashBalance: 0m,
                MonthlySpend: 0m,
                RemainingBudget: 0m,
                BudgetHealthPercent: 0m,
                GoalCount: 0,
                GoalsOnTrackCount: 0,
                GoalsBehindCount: 0,
                RecentTransactions: [])
        };
        var viewModel = new DashboardViewModel(service, autoLoad: false);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasError);
        Assert.True(viewModel.IsEmpty);
        Assert.False(viewModel.IsKpiEmpty);
        Assert.True(viewModel.IsRecentTransactionsEmpty);
        Assert.Empty(viewModel.RecentTransactions);
        Assert.Equal(5, viewModel.KpiCards.Count);
    }

    [Fact]
    public async Task LoadCommand_WithKpiDataButNoTransactions_SetsIndependentEmptyFlags()
    {
        var service = new FakeDashboardDataService
        {
            Workspace = new DashboardWorkspaceData(
                AccountCount: 1,
                NetWorth: 1000m,
                CashBalance: 1000m,
                MonthlySpend: 0m,
                RemainingBudget: 500m,
                BudgetHealthPercent: 50m,
                GoalCount: 1,
                GoalsOnTrackCount: 1,
                GoalsBehindCount: 0,
                RecentTransactions: [])
        };
        var viewModel = new DashboardViewModel(service, autoLoad: false);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasError);
        Assert.False(viewModel.IsEmpty);
        Assert.False(viewModel.IsKpiEmpty);
        Assert.True(viewModel.IsRecentTransactionsEmpty);
        Assert.Equal(5, viewModel.KpiCards.Count);
        Assert.Empty(viewModel.RecentTransactions);
    }

    [Fact]
    public async Task LoadCommand_ServiceFailure_SetsErrorAndClearsData()
    {
        var service = new FakeDashboardDataService
        {
            ExceptionToThrow = new InvalidOperationException("backend unavailable")
        };
        var viewModel = new DashboardViewModel(service, autoLoad: false);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Contains("Dashboard load failed", viewModel.ErrorMessage, StringComparison.Ordinal);
        Assert.True(viewModel.IsKpiEmpty);
        Assert.True(viewModel.IsRecentTransactionsEmpty);
        Assert.Empty(viewModel.KpiCards);
        Assert.Empty(viewModel.RecentTransactions);
        Assert.True(viewModel.IsEmpty);
    }

    private sealed class FakeDashboardDataService : IDashboardDataService
    {
        public DashboardWorkspaceData Workspace { get; init; } = new(
            AccountCount: 0,
            NetWorth: 0m,
            CashBalance: 0m,
            MonthlySpend: 0m,
            RemainingBudget: 0m,
            BudgetHealthPercent: 0m,
            GoalCount: 0,
            GoalsOnTrackCount: 0,
            GoalsBehindCount: 0,
            RecentTransactions: []);

        public Exception? ExceptionToThrow { get; init; }

        public Task<DashboardWorkspaceData> GetWorkspaceAsync(CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(Workspace);
        }
    }
}
