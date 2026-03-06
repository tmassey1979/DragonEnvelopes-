using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using DragonEnvelopes.Contracts.Accounts;
using DragonEnvelopes.Contracts.Imports;
using DragonEnvelopes.Desktop.Api;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public sealed class ImportsDataService : IImportsDataService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IBackendApiClient _apiClient;
    private readonly IFamilyContext _familyContext;

    public ImportsDataService(IBackendApiClient apiClient, IFamilyContext familyContext)
    {
        _apiClient = apiClient;
        _familyContext = familyContext;
    }

    public async Task<IReadOnlyList<AccountListItemViewModel>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        using var response = await _apiClient.GetAsync($"accounts?familyId={familyId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Accounts request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var accounts = await JsonSerializer.DeserializeAsync<List<AccountResponse>>(stream, SerializerOptions, cancellationToken)
            ?? [];
        return accounts
            .OrderBy(static account => account.Name, StringComparer.OrdinalIgnoreCase)
            .Select(static account => new AccountListItemViewModel(
                account.Id,
                account.Name,
                account.Type,
                account.Balance.ToString("$#,##0.00")))
            .ToArray();
    }

    public async Task<ImportPreviewResultData> PreviewAsync(
        Guid accountId,
        string csvContent,
        string? delimiter,
        IReadOnlyDictionary<string, string>? headerMappings,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new ImportPreviewRequest(
            familyId,
            accountId,
            csvContent,
            delimiter,
            headerMappings);

        using var request = new HttpRequestMessage(HttpMethod.Post, "imports/transactions/preview")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };
        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Import preview failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync<ImportPreviewResponse>(stream, SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("Import preview returned empty response.");

        return new ImportPreviewResultData(
            result.Parsed,
            result.Valid,
            result.Deduped,
            result.Rows.Select(static row => new ImportPreviewRowData(
                    row.RowNumber,
                    row.OccurredOn?.ToString("yyyy-MM-dd") ?? "-",
                    row.Amount?.ToString("$#,##0.00") ?? "-",
                    row.Merchant ?? string.Empty,
                    row.Description ?? string.Empty,
                    row.Category ?? string.Empty,
                    row.IsDuplicate,
                    row.Errors.Count == 0 ? string.Empty : string.Join("; ", row.Errors)))
                .ToArray());
    }

    public async Task<ImportCommitResultData> CommitAsync(
        Guid accountId,
        string csvContent,
        string? delimiter,
        IReadOnlyDictionary<string, string>? headerMappings,
        IReadOnlyList<int>? acceptedRowNumbers,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new ImportCommitRequest(
            familyId,
            accountId,
            csvContent,
            delimiter,
            headerMappings,
            acceptedRowNumbers);

        using var request = new HttpRequestMessage(HttpMethod.Post, "imports/transactions/commit")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };
        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Import commit failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync<ImportCommitResponse>(stream, SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("Import commit returned empty response.");

        return new ImportCommitResultData(
            result.Parsed,
            result.Valid,
            result.Deduped,
            result.Inserted,
            result.Failed);
    }

    private Guid RequireFamilyId()
    {
        if (!_familyContext.FamilyId.HasValue)
        {
            throw new InvalidOperationException("No family selected for imports.");
        }

        return _familyContext.FamilyId.Value;
    }
}
