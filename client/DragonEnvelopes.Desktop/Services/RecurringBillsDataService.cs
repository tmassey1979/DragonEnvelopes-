using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using DragonEnvelopes.Contracts.RecurringBills;
using DragonEnvelopes.Desktop.Api;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public sealed class RecurringBillsDataService : IRecurringBillsDataService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IBackendApiClient _apiClient;
    private readonly IFamilyContext _familyContext;

    public RecurringBillsDataService(IBackendApiClient apiClient, IFamilyContext familyContext)
    {
        _apiClient = apiClient;
        _familyContext = familyContext;
    }

    public async Task<IReadOnlyList<RecurringBillItemViewModel>> GetBillsAsync(CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        using var response = await _apiClient.GetAsync($"recurring-bills?familyId={familyId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Recurring bills request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var bills = await JsonSerializer.DeserializeAsync<List<RecurringBillResponse>>(stream, SerializerOptions, cancellationToken)
            ?? [];

        return bills
            .OrderBy(static bill => bill.Name, StringComparer.OrdinalIgnoreCase)
            .Select(MapBill)
            .ToArray();
    }

    public async Task<RecurringBillItemViewModel> CreateBillAsync(
        string name,
        string merchant,
        decimal amount,
        string frequency,
        int dayOfMonth,
        DateOnly startDate,
        DateOnly? endDate,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new CreateRecurringBillRequest(
            familyId,
            name,
            merchant,
            amount,
            frequency,
            dayOfMonth,
            startDate,
            endDate,
            isActive);

        using var request = new HttpRequestMessage(HttpMethod.Post, "recurring-bills")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };
        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Create recurring bill failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var bill = await JsonSerializer.DeserializeAsync<RecurringBillResponse>(stream, SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("Create recurring bill returned empty response.");
        return MapBill(bill);
    }

    public async Task<RecurringBillItemViewModel> UpdateBillAsync(
        Guid id,
        string name,
        string merchant,
        decimal amount,
        string frequency,
        int dayOfMonth,
        DateOnly startDate,
        DateOnly? endDate,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var payload = new UpdateRecurringBillRequest(
            name,
            merchant,
            amount,
            frequency,
            dayOfMonth,
            startDate,
            endDate,
            isActive);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"recurring-bills/{id}")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };
        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Update recurring bill failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var bill = await JsonSerializer.DeserializeAsync<RecurringBillResponse>(stream, SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("Update recurring bill returned empty response.");
        return MapBill(bill);
    }

    public async Task DeleteBillAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"recurring-bills/{id}");
        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Delete recurring bill failed with status {(int)response.StatusCode}.");
        }
    }

    public async Task<IReadOnlyList<RecurringBillProjectionItemViewModel>> GetProjectionAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        using var response = await _apiClient.GetAsync(
            $"recurring-bills/projection?familyId={familyId}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Recurring bill projection failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var items = await JsonSerializer.DeserializeAsync<List<RecurringBillProjectionItemResponse>>(stream, SerializerOptions, cancellationToken)
            ?? [];
        return items
            .OrderBy(static item => item.DueDate)
            .ThenBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(static item => new RecurringBillProjectionItemViewModel(
                item.RecurringBillId,
                item.Name,
                item.Merchant,
                item.Amount.ToString("$#,##0.00"),
                item.DueDate.ToString("yyyy-MM-dd")))
            .ToArray();
    }

    public async Task<IReadOnlyList<RecurringBillExecutionItemViewModel>> GetExecutionHistoryAsync(
        Guid recurringBillId,
        int take = 25,
        CancellationToken cancellationToken = default)
    {
        var normalizedTake = Math.Clamp(take, 1, 100);
        using var response = await _apiClient.GetAsync(
            $"recurring-bills/{recurringBillId}/executions?take={normalizedTake}",
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Recurring bill execution history request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var items = await JsonSerializer.DeserializeAsync<List<RecurringBillExecutionResponse>>(
            stream,
            SerializerOptions,
            cancellationToken) ?? [];

        return items
            .OrderByDescending(static item => item.ExecutedAtUtc)
            .Select(static item => new RecurringBillExecutionItemViewModel(
                item.Id,
                item.DueDate.ToString("yyyy-MM-dd"),
                item.ExecutedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                item.Result,
                item.TransactionId?.ToString() ?? "-",
                item.IdempotencyKey,
                string.IsNullOrWhiteSpace(item.Notes) ? "-" : item.Notes))
            .ToArray();
    }

    private Guid RequireFamilyId()
    {
        if (!_familyContext.FamilyId.HasValue)
        {
            throw new InvalidOperationException("No family selected for recurring bill management.");
        }

        return _familyContext.FamilyId.Value;
    }

    private static RecurringBillItemViewModel MapBill(RecurringBillResponse bill)
    {
        return new RecurringBillItemViewModel(
            bill.Id,
            bill.Name,
            bill.Merchant,
            bill.Amount,
            bill.Amount.ToString("$#,##0.00"),
            bill.Frequency,
            bill.DayOfMonth,
            bill.StartDate.ToString("yyyy-MM-dd"),
            bill.EndDate?.ToString("yyyy-MM-dd") ?? "-",
            bill.IsActive);
    }
}
