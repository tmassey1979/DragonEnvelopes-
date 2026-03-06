using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class ReportsViewModel : ObservableObject
{
    private readonly IReportsDataService _reportsDataService;

    public ReportsViewModel(IReportsDataService reportsDataService)
    {
        _reportsDataService = reportsDataService;
        SelectedMonth = DateTime.UtcNow.ToString("yyyy-MM");
        RangeFrom = DateTime.UtcNow.AddMonths(-5).ToString("yyyy-MM-01");
        RangeTo = DateTime.UtcNow.ToString("yyyy-MM-dd");

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        ApplyFiltersCommand = new AsyncRelayCommand(LoadAsync);
        _ = LoadCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand ApplyFiltersCommand { get; }

    [ObservableProperty]
    private string selectedMonth = string.Empty;

    [ObservableProperty]
    private bool includeArchived;

    [ObservableProperty]
    private string rangeFrom = string.Empty;

    [ObservableProperty]
    private string rangeTo = string.Empty;

    [ObservableProperty]
    private ObservableCollection<MetricTileViewModel> summaryTiles = [];

    [ObservableProperty]
    private ObservableCollection<ReportEnvelopeBalanceRowViewModel> envelopeBalances = [];

    [ObservableProperty]
    private ObservableCollection<ReportMonthlySpendRowViewModel> monthlySpendRows = [];

    [ObservableProperty]
    private ObservableCollection<ReportCategoryBreakdownRowViewModel> categoryBreakdownRows = [];

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private string noDataMessage = "No report data exists for the selected filter.";

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            if (!DateTimeOffset.TryParse(RangeFrom, out var from) || !DateTimeOffset.TryParse(RangeTo, out var to))
            {
                HasError = true;
                ErrorMessage = "Date range is invalid.";
                SummaryTiles.Clear();
                EnvelopeBalances.Clear();
                MonthlySpendRows.Clear();
                CategoryBreakdownRows.Clear();
                IsEmpty = true;
                return;
            }

            var workspace = await _reportsDataService.GetWorkspaceAsync(SelectedMonth, from, to, IncludeArchived, cancellationToken);
            if (workspace.Summary is null && workspace.EnvelopeBalances.Count == 0 && workspace.MonthlySpend.Count == 0 && workspace.CategoryBreakdown.Count == 0)
            {
                SummaryTiles.Clear();
                EnvelopeBalances.Clear();
                MonthlySpendRows.Clear();
                CategoryBreakdownRows.Clear();
                IsEmpty = true;
                return;
            }

            var summary = workspace.Summary ?? new ReportSummaryData(0m, 0m, 0m, 0m);
            SummaryTiles = new ObservableCollection<MetricTileViewModel>(
            [
                new MetricTileViewModel("Net Worth", FormatCurrency(summary.NetWorth), "From all accounts", MetricTrendDirection.Neutral),
                new MetricTileViewModel("Monthly Spend", FormatCurrency(summary.MonthlySpend), $"Month {SelectedMonth}", MetricTrendDirection.Negative),
                new MetricTileViewModel("Remaining Budget", FormatCurrency(summary.RemainingBudget), "Available after spend", MetricTrendDirection.Positive),
                new MetricTileViewModel("Envelope Coverage", $"{summary.EnvelopeCoveragePercent:0.0}%", "Budget allocation coverage", MetricTrendDirection.Neutral)
            ]);

            EnvelopeBalances = new ObservableCollection<ReportEnvelopeBalanceRowViewModel>(
                workspace.EnvelopeBalances.Select(static row => new ReportEnvelopeBalanceRowViewModel(
                    row.EnvelopeName,
                    row.MonthlyBudget.ToString("$#,##0.00"),
                    row.CurrentBalance.ToString("$#,##0.00"),
                    row.IsArchived)));

            MonthlySpendRows = new ObservableCollection<ReportMonthlySpendRowViewModel>(
                workspace.MonthlySpend.Select(static row => new ReportMonthlySpendRowViewModel(
                    row.Month,
                    row.TotalSpend.ToString("$#,##0.00"))));

            CategoryBreakdownRows = new ObservableCollection<ReportCategoryBreakdownRowViewModel>(
                workspace.CategoryBreakdown.Select(static row => new ReportCategoryBreakdownRowViewModel(
                    row.Category,
                    row.TotalSpend.ToString("$#,##0.00"))));

            IsEmpty = SummaryTiles.Count == 0
                && EnvelopeBalances.Count == 0
                && MonthlySpendRows.Count == 0
                && CategoryBreakdownRows.Count == 0;
        }
        catch (OperationCanceledException)
        {
            HasError = true;
            ErrorMessage = "Report load canceled.";
            SummaryTiles.Clear();
            EnvelopeBalances.Clear();
            MonthlySpendRows.Clear();
            CategoryBreakdownRows.Clear();
            IsEmpty = true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Report load failed: {ex.Message}";
            SummaryTiles.Clear();
            EnvelopeBalances.Clear();
            MonthlySpendRows.Clear();
            CategoryBreakdownRows.Clear();
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
