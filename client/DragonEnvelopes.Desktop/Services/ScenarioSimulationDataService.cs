using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using DragonEnvelopes.Contracts.Scenarios;
using DragonEnvelopes.Desktop.Api;

namespace DragonEnvelopes.Desktop.Services;

public sealed class ScenarioSimulationDataService : IScenarioSimulationDataService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IBackendApiClient _apiClient;
    private readonly IFamilyContext _familyContext;

    public ScenarioSimulationDataService(IBackendApiClient apiClient, IFamilyContext familyContext)
    {
        _apiClient = apiClient;
        _familyContext = familyContext;
    }

    public async Task<ScenarioSimulationWorkspaceData> SimulateAsync(
        decimal monthlyIncome,
        decimal fixedExpenses,
        decimal? discretionaryCutPercent,
        int monthHorizon,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new SimulateScenarioRequest(
            familyId,
            monthlyIncome,
            fixedExpenses,
            discretionaryCutPercent,
            monthHorizon);

        using var request = new HttpRequestMessage(HttpMethod.Post, "scenarios/simulate")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };
        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Scenario simulation failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var simulation = await JsonSerializer.DeserializeAsync<ScenarioSimulationResponse>(stream, SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("Scenario simulation returned empty response.");

        return new ScenarioSimulationWorkspaceData(
            simulation.FamilyId,
            simulation.StartingBalance,
            simulation.MonthlyIncome,
            simulation.FixedExpenses,
            simulation.EffectiveExpenses,
            simulation.NetMonthlyChange,
            simulation.MonthHorizon,
            simulation.DepletionMonth,
            simulation.EndingBalance,
            simulation.Months
                .OrderBy(static month => month.MonthIndex)
                .Select(static month => new ScenarioSimulationMonthData(
                    month.MonthIndex,
                    month.Month,
                    month.Income,
                    month.Expenses,
                    month.ProjectedBalance))
                .ToArray());
    }

    private Guid RequireFamilyId()
    {
        if (!_familyContext.FamilyId.HasValue)
        {
            throw new InvalidOperationException("No family selected for scenario simulation.");
        }

        return _familyContext.FamilyId.Value;
    }
}
