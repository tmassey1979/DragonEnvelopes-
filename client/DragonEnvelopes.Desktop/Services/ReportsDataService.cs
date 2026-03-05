using System.Text.Json;
using DragonEnvelopes.Desktop.Api;

namespace DragonEnvelopes.Desktop.Services;

public sealed class ReportsDataService : IReportsDataService
{
    private readonly IBackendApiClient _apiClient;

    public ReportsDataService(IBackendApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ReportSummaryData?> GetSummaryAsync(
        string month,
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        var encodedMonth = Uri.EscapeDataString(month);
        var requestPath = $"reports/summary?month={encodedMonth}&includeArchived={includeArchived.ToString().ToLowerInvariant()}";

        using var response = await _apiClient.GetAsync(requestPath, cancellationToken);
        if (response.StatusCode is System.Net.HttpStatusCode.NotFound
            or System.Net.HttpStatusCode.NoContent)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Reports API returned status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        if (!TryGetDecimal(root, "netWorth", out var netWorth)
            || !TryGetDecimal(root, "monthlySpend", out var monthlySpend)
            || !TryGetDecimal(root, "remainingBudget", out var remainingBudget)
            || !TryGetDecimal(root, "envelopeCoveragePercent", out var envelopeCoveragePercent))
        {
            return null;
        }

        return new ReportSummaryData(netWorth, monthlySpend, remainingBudget, envelopeCoveragePercent);
    }

    private static bool TryGetDecimal(JsonElement root, string propertyName, out decimal value)
    {
        value = default;
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return false;
        }

        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetDecimal(out value),
            JsonValueKind.String => decimal.TryParse(element.GetString(), out value),
            _ => false
        };
    }
}
