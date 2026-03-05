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
    private ObservableCollection<MetricTileViewModel> summaryTiles = [];

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
            var summary = await _reportsDataService.GetSummaryAsync(SelectedMonth, IncludeArchived, cancellationToken);
            if (summary is null)
            {
                SummaryTiles.Clear();
                IsEmpty = true;
                return;
            }

            SummaryTiles = new ObservableCollection<MetricTileViewModel>(
            [
                new MetricTileViewModel("Net Worth", FormatCurrency(summary.NetWorth), "From all accounts", MetricTrendDirection.Neutral),
                new MetricTileViewModel("Monthly Spend", FormatCurrency(summary.MonthlySpend), $"Month {SelectedMonth}", MetricTrendDirection.Negative),
                new MetricTileViewModel("Remaining Budget", FormatCurrency(summary.RemainingBudget), "Available after spend", MetricTrendDirection.Positive),
                new MetricTileViewModel("Envelope Coverage", $"{summary.EnvelopeCoveragePercent:0.0}%", "Budget allocation coverage", MetricTrendDirection.Neutral)
            ]);

            IsEmpty = SummaryTiles.Count == 0;
        }
        catch (OperationCanceledException)
        {
            HasError = true;
            ErrorMessage = "Report load canceled.";
            SummaryTiles.Clear();
            IsEmpty = true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Report load failed: {ex.Message}";
            SummaryTiles.Clear();
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
