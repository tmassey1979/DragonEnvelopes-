using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly IDashboardDataService _dashboardDataService;

    public DashboardViewModel(IDashboardDataService dashboardDataService, bool autoLoad = true)
    {
        _dashboardDataService = dashboardDataService;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        if (autoLoad)
        {
            _ = LoadCommand.ExecuteAsync(null);
        }
    }

    public IAsyncRelayCommand LoadCommand { get; }

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private bool isKpiEmpty;

    [ObservableProperty]
    private bool isRecentTransactionsEmpty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<MetricTileViewModel> kpiCards = [];

    [ObservableProperty]
    private ObservableCollection<TransactionRowViewModel> recentTransactions = [];

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        IsKpiEmpty = false;
        IsRecentTransactionsEmpty = false;
        ErrorMessage = string.Empty;

        try
        {
            var workspace = await _dashboardDataService.GetWorkspaceAsync(cancellationToken);

            var kpis = new[]
            {
                new MetricTileViewModel("Net Worth", FormatCurrency(workspace.NetWorth), "Across all linked accounts", MetricTrendDirection.Neutral),
                new MetricTileViewModel("Cash Balance", FormatCurrency(workspace.CashBalance), "Checking, savings, and cash accounts", MetricTrendDirection.Neutral),
                new MetricTileViewModel("Monthly Spend", FormatCurrency(workspace.MonthlySpend), "Current month spend", MetricTrendDirection.Negative),
                new MetricTileViewModel(
                    "Budget Health",
                    $"{workspace.BudgetHealthPercent:0.0}%",
                    $"Remaining {FormatCurrency(workspace.RemainingBudget)}",
                    workspace.BudgetHealthPercent >= 50m
                        ? MetricTrendDirection.Positive
                        : MetricTrendDirection.Neutral),
                new MetricTileViewModel(
                    "Envelope Goals",
                    workspace.GoalCount == 0
                        ? "No goals"
                        : $"{workspace.GoalsOnTrackCount}/{workspace.GoalCount} on track",
                    workspace.GoalCount == 0
                        ? "Define envelope goals to monitor progress."
                        : $"{workspace.GoalsBehindCount} behind schedule",
                    workspace.GoalCount == 0 || workspace.GoalsBehindCount == 0
                        ? MetricTrendDirection.Positive
                        : MetricTrendDirection.Negative)
            };

            var transactions = workspace.RecentTransactions
                .Select(transaction => new TransactionRowViewModel(
                    transaction.OccurredAt.ToString("yyyy-MM-dd"),
                    transaction.Merchant,
                    FormatCurrency(transaction.Amount),
                    transaction.EnvelopeName,
                    transaction.Category))
                .ToArray();

            KpiCards = new ObservableCollection<MetricTileViewModel>(kpis);
            RecentTransactions = new ObservableCollection<TransactionRowViewModel>(transactions);
            IsKpiEmpty = KpiCards.Count == 0;
            IsRecentTransactionsEmpty = RecentTransactions.Count == 0;
            IsEmpty = workspace.AccountCount == 0 && RecentTransactions.Count == 0;
        }
        catch (OperationCanceledException)
        {
            HasError = true;
            ErrorMessage = "Dashboard load canceled.";
            KpiCards.Clear();
            RecentTransactions.Clear();
            IsKpiEmpty = true;
            IsRecentTransactionsEmpty = true;
            IsEmpty = true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Dashboard load failed: {ex.Message}";
            KpiCards.Clear();
            RecentTransactions.Clear();
            IsKpiEmpty = true;
            IsRecentTransactionsEmpty = true;
            IsEmpty = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string FormatCurrency(decimal amount)
    {
        return amount.ToString("$#,##0.00");
    }
}
