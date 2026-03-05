using System.Text.Json;
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
                transaction.EnvelopeId.HasValue && envelopeLookup.TryGetValue(transaction.EnvelopeId.Value, out var envelopeName)
                    ? envelopeName
                    : "-"))
            .ToArray();
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
