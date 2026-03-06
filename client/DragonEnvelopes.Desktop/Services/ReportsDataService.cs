using System.Text.Json;
using DragonEnvelopes.Contracts.Reports;
using DragonEnvelopes.Desktop.Api;

namespace DragonEnvelopes.Desktop.Services;

public sealed class ReportsDataService : IReportsDataService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IBackendApiClient _apiClient;
    private readonly IFamilyContext _familyContext;

    public ReportsDataService(IBackendApiClient apiClient, IFamilyContext familyContext)
    {
        _apiClient = apiClient;
        _familyContext = familyContext;
    }

    public async Task<ReportWorkspaceData> GetWorkspaceAsync(
        string month,
        DateTimeOffset from,
        DateTimeOffset to,
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var encodedMonth = Uri.EscapeDataString(month);
        using var remainingResponse = await _apiClient.GetAsync(
            $"reports/remaining-budget?familyId={familyId}&month={encodedMonth}",
            cancellationToken);

        ReportSummaryData? summary = null;
        if (remainingResponse.IsSuccessStatusCode)
        {
            await using var stream = await remainingResponse.Content.ReadAsStreamAsync(cancellationToken);
            var remaining = await JsonSerializer.DeserializeAsync<RemainingBudgetReportResponse>(stream, SerializerOptions, cancellationToken);
            if (remaining is not null)
            {
                summary = new ReportSummaryData(
                    NetWorth: 0m,
                    MonthlySpend: remaining.TotalIncome - remaining.RemainingAmount,
                    RemainingBudget: remaining.RemainingAmount,
                    EnvelopeCoveragePercent: remaining.TotalIncome == 0m
                        ? 0m
                        : decimal.Round((remaining.AllocatedAmount / remaining.TotalIncome) * 100m, 1, MidpointRounding.AwayFromZero));
            }
        }
        else if (remainingResponse.StatusCode is not System.Net.HttpStatusCode.NotFound
                 and not System.Net.HttpStatusCode.NoContent)
        {
            throw new InvalidOperationException($"Remaining budget report request failed with status {(int)remainingResponse.StatusCode}.");
        }

        using var envelopeResponse = await _apiClient.GetAsync(
            $"reports/envelope-balances?familyId={familyId}",
            cancellationToken);
        if (!envelopeResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Envelope balances report request failed with status {(int)envelopeResponse.StatusCode}.");
        }

        await using var envelopeStream = await envelopeResponse.Content.ReadAsStreamAsync(cancellationToken);
        var envelopeRows = await JsonSerializer.DeserializeAsync<List<EnvelopeBalanceReportResponse>>(
            envelopeStream,
            SerializerOptions,
            cancellationToken) ?? [];

        using var monthlySpendResponse = await _apiClient.GetAsync(
            $"reports/monthly-spend?familyId={familyId}&from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}",
            cancellationToken);
        if (!monthlySpendResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Monthly spend report request failed with status {(int)monthlySpendResponse.StatusCode}.");
        }

        await using var monthlyStream = await monthlySpendResponse.Content.ReadAsStreamAsync(cancellationToken);
        var monthlyRows = await JsonSerializer.DeserializeAsync<List<MonthlySpendReportPointResponse>>(
            monthlyStream,
            SerializerOptions,
            cancellationToken) ?? [];

        using var categoryResponse = await _apiClient.GetAsync(
            $"reports/category-breakdown?familyId={familyId}&from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}",
            cancellationToken);
        if (!categoryResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Category breakdown report request failed with status {(int)categoryResponse.StatusCode}.");
        }

        await using var categoryStream = await categoryResponse.Content.ReadAsStreamAsync(cancellationToken);
        var categoryRows = await JsonSerializer.DeserializeAsync<List<CategoryBreakdownReportItemResponse>>(
            categoryStream,
            SerializerOptions,
            cancellationToken) ?? [];

        return new ReportWorkspaceData(
            summary,
            envelopeRows
                .Where(row => includeArchived || !row.IsArchived)
                .OrderByDescending(static row => row.CurrentBalance)
                .Select(static row => new ReportEnvelopeBalanceData(row.EnvelopeName, row.MonthlyBudget, row.CurrentBalance, row.IsArchived))
                .ToArray(),
            monthlyRows
                .OrderBy(static row => row.Month, StringComparer.OrdinalIgnoreCase)
                .Select(static row => new ReportMonthlySpendData(row.Month, row.TotalSpend))
                .ToArray(),
            categoryRows
                .OrderByDescending(static row => row.TotalSpend)
                .Select(static row => new ReportCategoryBreakdownData(row.Category, row.TotalSpend))
                .ToArray());
    }

    private Guid RequireFamilyId()
    {
        if (!_familyContext.FamilyId.HasValue)
        {
            throw new InvalidOperationException("No family selected for reports.");
        }

        return _familyContext.FamilyId.Value;
    }
}
