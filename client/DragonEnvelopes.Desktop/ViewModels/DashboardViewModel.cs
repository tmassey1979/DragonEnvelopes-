using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class DashboardViewModel : ObservableObject
{
    public DashboardViewModel()
    {
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        _ = LoadCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadCommand { get; }

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private bool isEmpty;

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
        ErrorMessage = string.Empty;

        try
        {
            await Task.Delay(450, cancellationToken);

            var kpis = new[]
            {
                new MetricTileViewModel("Net Worth", FormatCurrency(18420.73m), "+3.8% month-over-month", MetricTrendDirection.Positive),
                new MetricTileViewModel("Cash Balance", FormatCurrency(4560.10m), "+$220 this week", MetricTrendDirection.Positive),
                new MetricTileViewModel("Budget Health", "81%", "Stable", MetricTrendDirection.Neutral)
            };

            var transactions = new[]
            {
                new TransactionRowViewModel("2026-03-04", "Trader Joe's", FormatCurrency(64.31m), "Groceries", "Food"),
                new TransactionRowViewModel("2026-03-03", "ComEd", FormatCurrency(118.07m), "Utilities", "Bills"),
                new TransactionRowViewModel("2026-03-02", "Fuel Station", FormatCurrency(39.12m), "Fuel", "Transport")
            };

            KpiCards = new ObservableCollection<MetricTileViewModel>(kpis);
            RecentTransactions = new ObservableCollection<TransactionRowViewModel>(transactions);
            IsEmpty = KpiCards.Count == 0 && RecentTransactions.Count == 0;
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Dashboard load canceled.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Dashboard load failed: {ex.Message}";
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
