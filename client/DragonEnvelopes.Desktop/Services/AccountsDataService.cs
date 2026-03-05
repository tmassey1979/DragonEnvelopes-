using System.Text.Json;
using DragonEnvelopes.Contracts.Accounts;
using DragonEnvelopes.Desktop.Api;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public sealed class AccountsDataService : IAccountsDataService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IBackendApiClient _apiClient;
    private readonly IFamilyContext _familyContext;

    public AccountsDataService(IBackendApiClient apiClient, IFamilyContext familyContext)
    {
        _apiClient = apiClient;
        _familyContext = familyContext;
    }

    public async Task<IReadOnlyList<AccountListItemViewModel>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        if (!_familyContext.FamilyId.HasValue)
        {
            throw new InvalidOperationException("No family is selected for the current session.");
        }

        using var response = await _apiClient.GetAsync($"accounts?familyId={_familyContext.FamilyId.Value}", cancellationToken);
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
