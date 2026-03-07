using DragonEnvelopes.Desktop.Services;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Tests;

public sealed class ScenarioSimulatorViewModelTests
{
    [Fact]
    public async Task RunSimulationCommand_PopulatesProjectionRowsAndSummary()
    {
        var service = new FakeScenarioSimulationDataService();
        var exporter = new FakeScenarioSimulationCsvExporter();
        var viewModel = new ScenarioSimulatorViewModel(service, exporter);

        await viewModel.RunSimulationCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasError);
        Assert.False(viewModel.IsEmpty);
        Assert.Equal(2, viewModel.MonthPoints.Count);
        Assert.Contains("Start", viewModel.SimulationSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("depletion", viewModel.RunwaySummary, StringComparison.OrdinalIgnoreCase);
        Assert.True(viewModel.ChartMaximum > 0d);
    }

    [Fact]
    public async Task RunSimulationCommand_InvalidCutPercent_SetsValidationError()
    {
        var service = new FakeScenarioSimulationDataService();
        var exporter = new FakeScenarioSimulationCsvExporter();
        var viewModel = new ScenarioSimulatorViewModel(service, exporter)
        {
            DraftDiscretionaryCutPercent = "abc"
        };

        await viewModel.RunSimulationCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Contains("invalid", viewModel.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExportCsvCommand_WritesCsvFile()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "dragonenvelopes-scenario-tests", Guid.NewGuid().ToString("N"));
        var service = new FakeScenarioSimulationDataService();
        var exporter = new FakeScenarioSimulationCsvExporter();
        var viewModel = new ScenarioSimulatorViewModel(
            service,
            exporter,
            exportDirectoryProvider: () => tempRoot);

        await viewModel.RunSimulationCommand.ExecuteAsync(null);
        await viewModel.ExportCsvCommand.ExecuteAsync(null);

        var files = Directory.Exists(tempRoot)
            ? Directory.GetFiles(tempRoot, "*.csv")
            : [];
        try
        {
            Assert.False(viewModel.HasError);
            Assert.NotEmpty(files);
            Assert.Contains("Exported", viewModel.ExportSummary, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private sealed class FakeScenarioSimulationDataService : IScenarioSimulationDataService
    {
        public Task<ScenarioSimulationWorkspaceData> SimulateAsync(
            decimal monthlyIncome,
            decimal fixedExpenses,
            decimal? discretionaryCutPercent,
            int monthHorizon,
            CancellationToken cancellationToken = default)
        {
            var data = new ScenarioSimulationWorkspaceData(
                Guid.Parse("90000000-0000-0000-0000-000000000001"),
                StartingBalance: 400m,
                MonthlyIncome: monthlyIncome,
                FixedExpenses: fixedExpenses,
                EffectiveExpenses: 900m,
                NetMonthlyChange: -100m,
                MonthHorizon: 2,
                DepletionMonth: 2,
                EndingBalance: 200m,
                Months:
                [
                    new ScenarioSimulationMonthData(1, "2026-01", 1000m, 900m, 300m),
                    new ScenarioSimulationMonthData(2, "2026-02", 1000m, 900m, 200m)
                ]);

            return Task.FromResult(data);
        }
    }

    private sealed class FakeScenarioSimulationCsvExporter : IScenarioSimulationCsvExporter
    {
        public string BuildCsv(IReadOnlyList<ScenarioSimulationMonthPointViewModel> points)
        {
            return "MonthIndex,Month,Income,Expenses,ProjectedBalance\n1,2026-01,1000,900,300\n";
        }
    }
}
