using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public interface IScenarioSimulationCsvExporter
{
    string BuildCsv(IReadOnlyList<ScenarioSimulationMonthPointViewModel> points);
}
