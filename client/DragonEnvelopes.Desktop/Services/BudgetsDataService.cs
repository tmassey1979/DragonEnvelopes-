using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Json;
using DragonEnvelopes.Contracts.Budgets;
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

    public async Task<BudgetAllocationWorkspaceData> GetWorkspaceAsync(
        string month,
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var encodedMonth = Uri.EscapeDataString(month);
        RemainingBudgetReportResponse? budgetSummary = null;

        using var budgetResponse = await _apiClient.GetAsync(
            $"reports/remaining-budget?familyId={familyId}&month={encodedMonth}",
            cancellationToken);

        if (budgetResponse.StatusCode is not System.Net.HttpStatusCode.NotFound
            and not System.Net.HttpStatusCode.NoContent
            && !budgetResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Remaining budget request failed with status {(int)budgetResponse.StatusCode}.");
        }

        if (budgetResponse.IsSuccessStatusCode)
        {
            await using var budgetStream = await budgetResponse.Content.ReadAsStreamAsync(cancellationToken);
            budgetSummary = await JsonSerializer.DeserializeAsync<RemainingBudgetReportResponse>(
                budgetStream,
                SerializerOptions,
                cancellationToken);
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
            budgetSummary?.Month ?? month,
            budgetSummary?.TotalIncome ?? 0m,
            budgetSummary?.AllocatedAmount ?? 0m,
            budgetSummary?.RemainingAmount ?? 0m,
            envelopes);
    }

    public async Task<BudgetMonthSummaryData?> GetBudgetAsync(
        string month,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var encodedMonth = Uri.EscapeDataString(month);
        using var response = await _apiClient.GetAsync($"budgets/{familyId}/{encodedMonth}", cancellationToken);
        if (response.StatusCode is System.Net.HttpStatusCode.NotFound
            or System.Net.HttpStatusCode.NoContent)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Get budget request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var budget = await JsonSerializer.DeserializeAsync<BudgetResponse>(stream, SerializerOptions, cancellationToken);
        if (budget is null)
        {
            return null;
        }

        return new BudgetMonthSummaryData(
            budget.Id,
            budget.Month,
            budget.TotalIncome,
            budget.AllocatedAmount,
            budget.RemainingAmount);
    }

    public async Task<BudgetMonthSummaryData> CreateBudgetAsync(
        string month,
        decimal totalIncome,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new CreateBudgetRequest(familyId, month, totalIncome);
        using var request = new HttpRequestMessage(HttpMethod.Post, "budgets")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };

        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Create budget request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var budget = await JsonSerializer.DeserializeAsync<BudgetResponse>(stream, SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("Create budget returned empty response.");

        return new BudgetMonthSummaryData(
            budget.Id,
            budget.Month,
            budget.TotalIncome,
            budget.AllocatedAmount,
            budget.RemainingAmount);
    }

    public async Task<BudgetMonthSummaryData> UpdateBudgetAsync(
        Guid budgetId,
        decimal totalIncome,
        CancellationToken cancellationToken = default)
    {
        var payload = new UpdateBudgetRequest(totalIncome);
        using var request = new HttpRequestMessage(HttpMethod.Put, $"budgets/{budgetId}")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };

        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Update budget request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var budget = await JsonSerializer.DeserializeAsync<BudgetResponse>(stream, SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("Update budget returned empty response.");

        return new BudgetMonthSummaryData(
            budget.Id,
            budget.Month,
            budget.TotalIncome,
            budget.AllocatedAmount,
            budget.RemainingAmount);
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
