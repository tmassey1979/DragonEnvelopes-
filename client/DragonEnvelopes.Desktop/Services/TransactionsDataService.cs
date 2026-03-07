using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Json;
using DragonEnvelopes.Contracts.Accounts;
using DragonEnvelopes.Contracts.Envelopes;
using DragonEnvelopes.Contracts.Transactions;
using DragonEnvelopes.Desktop.Api;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public sealed class TransactionsDataService : ITransactionsDataService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IBackendApiClient _apiClient;
    private readonly IFamilyContext _familyContext;

    public TransactionsDataService(IBackendApiClient apiClient, IFamilyContext familyContext)
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

    public async Task<IReadOnlyList<TransactionListItemViewModel>> GetTransactionsAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        using var envelopesResponse = await _apiClient.GetAsync($"envelopes?familyId={familyId}", cancellationToken);
        if (!envelopesResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Envelopes request failed with status {(int)envelopesResponse.StatusCode}.");
        }

        await using var envelopesStream = await envelopesResponse.Content.ReadAsStreamAsync(cancellationToken);
        var envelopes = await JsonSerializer.DeserializeAsync<List<EnvelopeResponse>>(envelopesStream, SerializerOptions, cancellationToken)
            ?? [];
        var envelopeLookup = envelopes.ToDictionary(static envelope => envelope.Id, static envelope => envelope.Name);

        using var response = await _apiClient.GetAsync($"transactions?accountId={accountId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Transactions request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var transactions = await JsonSerializer.DeserializeAsync<List<TransactionResponse>>(stream, SerializerOptions, cancellationToken)
            ?? [];

        return transactions.Select(transaction =>
            new TransactionListItemViewModel(
                transaction.Id,
                transaction.AccountId,
                transaction.OccurredAt,
                transaction.Merchant,
                transaction.Description,
                transaction.Amount,
                transaction.Category,
                transaction.EnvelopeId,
                transaction.EnvelopeId.HasValue && envelopeLookup.TryGetValue(transaction.EnvelopeId.Value, out var envelopeName)
                    ? envelopeName
                    : "-",
                transaction.Splits.Select(static split => new TransactionSplitSnapshotViewModel(
                        split.EnvelopeId,
                        split.Amount,
                        split.Category,
                        split.Notes))
                    .ToArray()))
            .ToArray();
    }

    public async Task<IReadOnlyList<EnvelopeOptionViewModel>> GetEnvelopesAsync(CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        using var response = await _apiClient.GetAsync($"envelopes?familyId={familyId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Envelopes request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var envelopes = await JsonSerializer.DeserializeAsync<List<EnvelopeResponse>>(stream, SerializerOptions, cancellationToken)
            ?? [];
        return envelopes
            .Where(static envelope => !envelope.IsArchived)
            .OrderBy(static envelope => envelope.Name, StringComparer.OrdinalIgnoreCase)
            .Select(static envelope => new EnvelopeOptionViewModel(envelope.Id, envelope.Name))
            .ToArray();
    }

    public async Task CreateTransactionAsync(
        Guid accountId,
        decimal amount,
        string description,
        string merchant,
        DateTimeOffset occurredAt,
        string? category,
        Guid? envelopeId,
        IReadOnlyList<TransactionSplitDraftViewModel>? splits,
        CancellationToken cancellationToken = default)
    {
        var payload = new CreateTransactionRequest(
            accountId,
            amount,
            description,
            merchant,
            occurredAt,
            string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            envelopeId,
            splits is null
                ? null
                : splits.Select(static split => new TransactionSplitRequest(
                        split.EnvelopeId!.Value,
                        split.Amount,
                        string.IsNullOrWhiteSpace(split.Category) ? null : split.Category.Trim(),
                        string.IsNullOrWhiteSpace(split.Notes) ? null : split.Notes.Trim()))
                    .ToArray());

        using var request = new HttpRequestMessage(HttpMethod.Post, "transactions")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };

        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Create transaction failed with status {(int)response.StatusCode}.");
        }
    }

    public async Task UpdateTransactionAsync(
        Guid transactionId,
        string description,
        string merchant,
        string? category,
        bool replaceAllocation,
        Guid? envelopeId,
        IReadOnlyList<TransactionSplitDraftViewModel>? splits,
        CancellationToken cancellationToken = default)
    {
        var payload = new UpdateTransactionRequest(
            description,
            merchant,
            string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            replaceAllocation,
            replaceAllocation && (splits is null || splits.Count == 0) ? envelopeId : null,
            replaceAllocation && splits is { Count: > 0 }
                ? splits.Select(static split => new TransactionSplitRequest(
                        split.EnvelopeId!.Value,
                        split.Amount,
                        string.IsNullOrWhiteSpace(split.Category) ? null : split.Category.Trim(),
                        string.IsNullOrWhiteSpace(split.Notes) ? null : split.Notes.Trim()))
                    .ToArray()
                : null);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"transactions/{transactionId}")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };

        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Update transaction failed with status {(int)response.StatusCode}.");
        }
    }

    public async Task DeleteTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"transactions/{transactionId}");
        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Delete transaction failed with status {(int)response.StatusCode}.");
        }
    }

    private Guid RequireFamilyId()
    {
        if (!_familyContext.FamilyId.HasValue)
        {
            throw new InvalidOperationException("No family is selected for the current session.");
        }

        return _familyContext.FamilyId.Value;
    }
}
