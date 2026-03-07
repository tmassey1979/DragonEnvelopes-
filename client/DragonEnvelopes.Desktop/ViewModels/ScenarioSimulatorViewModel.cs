using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class ScenarioSimulatorViewModel : ObservableObject
{
    private readonly IScenarioSimulationDataService _scenarioSimulationDataService;
    private readonly IScenarioSimulationCsvExporter _scenarioSimulationCsvExporter;
    private readonly Func<string> _exportDirectoryProvider;
    private decimal _chartOffset;

    public ScenarioSimulatorViewModel(
        IScenarioSimulationDataService scenarioSimulationDataService,
        IScenarioSimulationCsvExporter scenarioSimulationCsvExporter,
        Func<string>? exportDirectoryProvider = null)
    {
        _scenarioSimulationDataService = scenarioSimulationDataService;
        _scenarioSimulationCsvExporter = scenarioSimulationCsvExporter;
        _exportDirectoryProvider = exportDirectoryProvider ?? DefaultExportDirectoryProvider;

        RunSimulationCommand = new AsyncRelayCommand(RunSimulationAsync);
        ExportCsvCommand = new AsyncRelayCommand(ExportCsvAsync);
        ResetCommand = new RelayCommand(Reset);

        _ = RunSimulationCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand RunSimulationCommand { get; }
    public IAsyncRelayCommand ExportCsvCommand { get; }
    public IRelayCommand ResetCommand { get; }

    [ObservableProperty]
    private decimal draftMonthlyIncome = 5000m;

    [ObservableProperty]
    private decimal draftFixedExpenses = 3200m;

    [ObservableProperty]
    private string draftDiscretionaryCutPercent = string.Empty;

    [ObservableProperty]
    private int draftMonthHorizon = 6;

    [ObservableProperty]
    private ObservableCollection<ScenarioSimulationMonthPointViewModel> monthPoints = [];

    [ObservableProperty]
    private double chartMaximum = 1d;

    [ObservableProperty]
    private string simulationSummary = "Run a scenario to project monthly balances.";

    [ObservableProperty]
    private string runwaySummary = "Runway summary will appear after simulation.";

    [ObservableProperty]
    private string exportSummary = "No CSV export generated.";

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isEmpty = true;

    [ObservableProperty]
    private string noDataMessage = "Configure assumptions and run a simulation.";

    private async Task RunSimulationAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (DraftMonthHorizon is < 1 or > 120)
        {
            HasError = true;
            ErrorMessage = "Month horizon must be between 1 and 120.";
            return;
        }

        if (!TryParseDiscretionaryCutPercent(out var discretionaryCutPercent))
        {
            return;
        }

        IsLoading = true;
        try
        {
            var workspace = await _scenarioSimulationDataService.SimulateAsync(
                DraftMonthlyIncome,
                DraftFixedExpenses,
                discretionaryCutPercent,
                DraftMonthHorizon,
                cancellationToken);

            var orderedMonths = workspace.Months
                .OrderBy(static month => month.MonthIndex)
                .ToArray();

            if (orderedMonths.Length == 0)
            {
                MonthPoints.Clear();
                IsEmpty = true;
                ChartMaximum = 1d;
                SimulationSummary = "No projection rows were returned.";
                RunwaySummary = "Runway summary unavailable.";
                return;
            }

            var minimumBalance = orderedMonths.Min(static month => month.ProjectedBalance);
            _chartOffset = minimumBalance < 0m ? -minimumBalance : 0m;
            var maxAdjustedBalance = orderedMonths.Max(month => month.ProjectedBalance + _chartOffset);
            ChartMaximum = maxAdjustedBalance <= 0m ? 1d : (double)maxAdjustedBalance;

            MonthPoints = new ObservableCollection<ScenarioSimulationMonthPointViewModel>(
                orderedMonths.Select(month => new ScenarioSimulationMonthPointViewModel(
                    month.MonthIndex,
                    month.Month,
                    month.Income,
                    FormatCurrency(month.Income),
                    month.Expenses,
                    FormatCurrency(month.Expenses),
                    month.ProjectedBalance,
                    FormatCurrency(month.ProjectedBalance),
                    (double)(month.ProjectedBalance + _chartOffset))));

            IsEmpty = false;
            SimulationSummary =
                $"Start {FormatCurrency(workspace.StartingBalance)} | Net {FormatCurrency(workspace.NetMonthlyChange)} / month | End {FormatCurrency(workspace.EndingBalance)} after {workspace.MonthHorizon} month(s).";
            RunwaySummary = workspace.DepletionMonth.HasValue
                ? $"Projected depletion month: {workspace.DepletionMonth.Value}."
                : "Projected balance remains non-negative for the selected horizon.";
        }
        catch (OperationCanceledException)
        {
            HasError = true;
            ErrorMessage = "Scenario simulation canceled.";
            MonthPoints.Clear();
            IsEmpty = true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Scenario simulation failed: {ex.Message}";
            MonthPoints.Clear();
            IsEmpty = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExportCsvAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (MonthPoints.Count == 0)
        {
            HasError = true;
            ErrorMessage = "Run simulation before exporting CSV.";
            return;
        }

        try
        {
            var csv = _scenarioSimulationCsvExporter.BuildCsv(MonthPoints.ToArray());
            var exportDirectory = _exportDirectoryProvider();
            Directory.CreateDirectory(exportDirectory);

            var fileName = $"scenario-simulation-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.csv";
            var filePath = Path.Combine(exportDirectory, fileName);
            await File.WriteAllTextAsync(filePath, csv, cancellationToken);

            ExportSummary = $"Exported {MonthPoints.Count} row(s) to {filePath}.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"CSV export failed: {ex.Message}";
        }
    }

    private void Reset()
    {
        DraftMonthlyIncome = 5000m;
        DraftFixedExpenses = 3200m;
        DraftDiscretionaryCutPercent = string.Empty;
        DraftMonthHorizon = 6;
        MonthPoints.Clear();
        ChartMaximum = 1d;
        SimulationSummary = "Run a scenario to project monthly balances.";
        RunwaySummary = "Runway summary will appear after simulation.";
        ExportSummary = "No CSV export generated.";
        HasError = false;
        ErrorMessage = string.Empty;
        IsEmpty = true;
    }

    private bool TryParseDiscretionaryCutPercent(out decimal? cutPercent)
    {
        cutPercent = null;
        if (string.IsNullOrWhiteSpace(DraftDiscretionaryCutPercent))
        {
            return true;
        }

        if (decimal.TryParse(
                DraftDiscretionaryCutPercent,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var invariantParsed)
            || decimal.TryParse(
                DraftDiscretionaryCutPercent,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out invariantParsed))
        {
            cutPercent = invariantParsed;
            return true;
        }

        HasError = true;
        ErrorMessage = "Discretionary cut percent is invalid.";
        return false;
    }

    private static string FormatCurrency(decimal amount)
    {
        return amount.ToString("$#,##0.00");
    }

    private static string DefaultExportDirectoryProvider()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DragonEnvelopes",
            "Exports");
    }
}
