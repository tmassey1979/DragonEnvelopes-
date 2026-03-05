using System.Text.Json;
using DragonEnvelopes.Contracts.Reports;
using DragonEnvelopes.Desktop.Api;

namespace DragonEnvelopes.Desktop.Services;

public sealed class BudgetsDataService : IBudgetsDataService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IBackendApiClient _apiClient;
    private readonly IFamilyContext _familyContext;

    public BudgetsDataService(IBackendApiClient apiClient, IFamilyContext familyContext)
    {
        _apiClient = apiClient;
        _familyContext = familyContext;
    }

    public async Task<BudgetAllocationWorkspaceData?> GetWorkspaceAsync(
        string month,
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var encodedMonth = Uri.EscapeDataString(month);

        using var budgetResponse = await _apiClient.GetAsync(
            $"reports/remaining-budget?familyId={familyId}&month={encodedMonth}",
            cancellationToken);

        if (budgetResponse.StatusCode is System.Net.HttpStatusCode.NotFound
            or System.Net.HttpStatusCode.NoContent)
        {
            return null;
        }

        if (!budgetResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Remaining budget request failed with status {(int)budgetResponse.StatusCode}.");
        }

        await using var budgetStream = await budgetResponse.Content.ReadAsStreamAsync(cancellationToken);
        var budgetSummary = await JsonSerializer.DeserializeAsync<RemainingBudgetReportResponse>(
            budgetStream,
            SerializerOptions,
            cancellationToken);

        if (budgetSummary is null)
        {
            return null;
        }

        using var envelopesResponse = await _apiClient.GetAsync(
            $"reports/envelope-balances?familyId={familyId}",
            cancellationToken);
        if (!envelopesResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Envelope balances request failed with status {(int)envelopesResponse.StatusCode}.");
        }

        await using var envelopesStream = await envelopesResponse.Content.ReadAsStreamAsync(cancellationToken);
        var envelopeBalances = await JsonSerializer.DeserializeAsync<List<EnvelopeBalanceReportResponse>>(
            envelopesStream,
            SerializerOptions,
            cancellationToken) ?? [];

        var envelopes = envelopeBalances
            .Where(envelope => includeArchived || !envelope.IsArchived)
            .OrderByDescending(static envelope => envelope.MonthlyBudget)
            .ThenBy(static envelope => envelope.EnvelopeName, StringComparer.OrdinalIgnoreCase)
            .Select(static envelope => new BudgetAllocationEnvelopeData(
                envelope.EnvelopeId,
                envelope.EnvelopeName,
                envelope.MonthlyBudget,
                envelope.CurrentBalance,
                envelope.IsArchived))
            .ToArray();

        return new BudgetAllocationWorkspaceData(
            budgetSummary.Month,
            budgetSummary.TotalIncome,
            budgetSummary.AllocatedAmount,
            budgetSummary.RemainingAmount,
            envelopes);
    }

    private Guid RequireFamilyId()
    {
        if (!_familyContext.FamilyId.HasValue)
        {
            throw new InvalidOperationException("No family selected for budget workspace.");
        }

        return _familyContext.FamilyId.Value;
    }
}
