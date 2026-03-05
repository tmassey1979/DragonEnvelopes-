using System.Text.Json;
using DragonEnvelopes.Contracts.Accounts;
using DragonEnvelopes.Desktop.Api;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public sealed class AccountsDataService : IAccountsDataService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IBackendApiClient _apiClient;

    public AccountsDataService(IBackendApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IReadOnlyList<AccountListItemViewModel>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _apiClient.GetAsync("accounts", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Accounts API request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var items = await JsonSerializer.DeserializeAsync<List<AccountResponse>>(stream, SerializerOptions, cancellationToken)
            ?? [];

        return items.Select(static account => new AccountListItemViewModel(
            account.Id,
            account.Name,
            account.Type,
            account.Balance.ToString("$#,##0.00")))
            .ToArray();
    }
}
