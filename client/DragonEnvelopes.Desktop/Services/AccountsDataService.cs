using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Json;
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
        var familyId = RequireFamilyId();
        using var response = await _apiClient.GetAsync($"accounts?familyId={familyId}", cancellationToken);
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

    public async Task CreateAccountAsync(
        string name,
        string type,
        decimal openingBalance,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new CreateAccountRequest(
            familyId,
            name,
            type,
            openingBalance);

        using var request = new HttpRequestMessage(HttpMethod.Post, "accounts")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };

        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Create account API request failed with status {(int)response.StatusCode}.");
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
