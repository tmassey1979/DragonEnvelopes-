using System.Text.Json;
using DragonEnvelopes.Contracts.Accounts;
using DragonEnvelopes.Contracts.Envelopes;
using DragonEnvelopes.Contracts.Reports;
using DragonEnvelopes.Contracts.Transactions;
using DragonEnvelopes.Desktop.Api;

namespace DragonEnvelopes.Desktop.Services;

public sealed class DashboardDataService : IDashboardDataService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IBackendApiClient _apiClient;
    private readonly IFamilyContext _familyContext;

    public DashboardDataService(IBackendApiClient apiClient, IFamilyContext familyContext)
    {
        _apiClient = apiClient;
        _familyContext = familyContext;
    }

    public async Task<DashboardWorkspaceData> GetWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var accounts = await GetAccountsAsync(familyId, cancellationToken);
        var envelopes = await GetEnvelopesAsync(familyId, cancellationToken);

        var monthlySpend = await GetMonthlySpendAsync(familyId, cancellationToken);
        var (remainingBudget, budgetHealthPercent) = await GetRemainingBudgetAsync(familyId, cancellationToken);
        var recentTransactions = await GetRecentTransactionsAsync(accounts, envelopes, cancellationToken);

        return new DashboardWorkspaceData(
            AccountCount: accounts.Count,
            NetWorth: accounts.Sum(static account => account.Balance),
            CashBalance: accounts
                .Where(static account => IsCashAccountType(account.Type))
                .Sum(static account => account.Balance),
            MonthlySpend: monthlySpend,
            RemainingBudget: remainingBudget,
            BudgetHealthPercent: budgetHealthPercent,
            RecentTransactions: recentTransactions);
    }

    private async Task<IReadOnlyList<AccountResponse>> GetAccountsAsync(Guid familyId, CancellationToken cancellationToken)
    {
        using var response = await _apiClient.GetAsync($"accounts?familyId={familyId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Accounts request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var accounts = await JsonSerializer.DeserializeAsync<List<AccountResponse>>(stream, SerializerOptions, cancellationToken)
            ?? [];
        return accounts;
    }

    private async Task<IReadOnlyDictionary<Guid, string>> GetEnvelopesAsync(Guid familyId, CancellationToken cancellationToken)
    {
        using var response = await _apiClient.GetAsync($"envelopes?familyId={familyId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Envelopes request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var envelopes = await JsonSerializer.DeserializeAsync<List<EnvelopeResponse>>(stream, SerializerOptions, cancellationToken)
            ?? [];
        return envelopes.ToDictionary(static envelope => envelope.Id, static envelope => envelope.Name);
    }

    private async Task<decimal> GetMonthlySpendAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var startOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        using var response = await _apiClient.GetAsync(
            $"reports/monthly-spend?familyId={familyId}&from={Uri.EscapeDataString(startOfMonth.ToString("o"))}&to={Uri.EscapeDataString(now.ToString("o"))}",
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Monthly spend request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var rows = await JsonSerializer.DeserializeAsync<List<MonthlySpendReportPointResponse>>(
            stream,
            SerializerOptions,
            cancellationToken) ?? [];

        return rows.Sum(static row => row.TotalSpend);
    }

    private async Task<(decimal RemainingBudget, decimal BudgetHealthPercent)> GetRemainingBudgetAsync(
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var month = DateTimeOffset.UtcNow.ToString("yyyy-MM");
        using var response = await _apiClient.GetAsync(
            $"reports/remaining-budget?familyId={familyId}&month={Uri.EscapeDataString(month)}",
            cancellationToken);
        if (response.StatusCode is System.Net.HttpStatusCode.NotFound
            or System.Net.HttpStatusCode.NoContent)
        {
            return (0m, 0m);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Remaining budget request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var report = await JsonSerializer.DeserializeAsync<RemainingBudgetReportResponse>(
            stream,
            SerializerOptions,
            cancellationToken);
        if (report is null)
        {
            return (0m, 0m);
        }

        var budgetHealthPercent = report.TotalIncome <= 0m
            ? 0m
            : decimal.Round((report.RemainingAmount / report.TotalIncome) * 100m, 1, MidpointRounding.AwayFromZero);

        return (report.RemainingAmount, budgetHealthPercent);
    }

    private async Task<IReadOnlyList<DashboardRecentTransactionData>> GetRecentTransactionsAsync(
        IReadOnlyList<AccountResponse> accounts,
        IReadOnlyDictionary<Guid, string> envelopes,
        CancellationToken cancellationToken)
    {
        var transactions = new List<TransactionResponse>();
        foreach (var account in accounts)
        {
            using var response = await _apiClient.GetAsync($"transactions?accountId={account.Id}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Transactions request failed with status {(int)response.StatusCode} for account {account.Id}.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var accountTransactions = await JsonSerializer.DeserializeAsync<List<TransactionResponse>>(
                stream,
                SerializerOptions,
                cancellationToken) ?? [];
            transactions.AddRange(accountTransactions);
        }

        return transactions
            .OrderByDescending(static transaction => transaction.OccurredAt)
            .Take(8)
            .Select(transaction => new DashboardRecentTransactionData(
                transaction.OccurredAt,
                string.IsNullOrWhiteSpace(transaction.Merchant)
                    ? transaction.Description
                    : transaction.Merchant,
                transaction.Amount,
                transaction.Category ?? "Uncategorized",
                transaction.EnvelopeId.HasValue && envelopes.TryGetValue(transaction.EnvelopeId.Value, out var envelopeName)
                    ? envelopeName
                    : "-"))
            .ToArray();
    }

    private Guid RequireFamilyId()
    {
        if (!_familyContext.FamilyId.HasValue)
        {
            throw new InvalidOperationException("No family selected for dashboard.");
        }

        return _familyContext.FamilyId.Value;
    }

    private static bool IsCashAccountType(string accountType)
    {
        if (string.IsNullOrWhiteSpace(accountType))
        {
            return false;
        }

        return accountType.Equals("Checking", StringComparison.OrdinalIgnoreCase)
            || accountType.Equals("Savings", StringComparison.OrdinalIgnoreCase)
            || accountType.Equals("Cash", StringComparison.OrdinalIgnoreCase);
    }
}
