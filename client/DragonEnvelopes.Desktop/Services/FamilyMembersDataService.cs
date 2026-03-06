using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using DragonEnvelopes.Contracts.Families;
using DragonEnvelopes.Desktop.Api;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public sealed class FamilyMembersDataService : IFamilyMembersDataService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IBackendApiClient _apiClient;
    private readonly IFamilyContext _familyContext;

    public FamilyMembersDataService(IBackendApiClient apiClient, IFamilyContext familyContext)
    {
        _apiClient = apiClient;
        _familyContext = familyContext;
    }

    public async Task<IReadOnlyList<FamilyMemberItemViewModel>> GetMembersAsync(CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        using var response = await _apiClient.GetAsync($"families/{familyId}/members", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Family members request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var members = await JsonSerializer.DeserializeAsync<List<FamilyMemberResponse>>(stream, SerializerOptions, cancellationToken)
            ?? [];

        return members
            .OrderBy(static member => member.Name, StringComparer.OrdinalIgnoreCase)
            .Select(static member => new FamilyMemberItemViewModel(
                member.Id,
                member.KeycloakUserId,
                member.Name,
                member.Email,
                member.Role))
            .ToArray();
    }

    public async Task<FamilyMemberItemViewModel> AddMemberAsync(
        string keycloakUserId,
        string name,
        string email,
        string role,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new AddFamilyMemberRequest(keycloakUserId, name, email, role);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"families/{familyId}/members")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };
        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Add family member request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var member = await JsonSerializer.DeserializeAsync<FamilyMemberResponse>(stream, SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("Add member returned an empty response.");

        return new FamilyMemberItemViewModel(
            member.Id,
            member.KeycloakUserId,
            member.Name,
            member.Email,
            member.Role);
    }

    private Guid RequireFamilyId()
    {
        if (!_familyContext.FamilyId.HasValue)
        {
            throw new InvalidOperationException("No family selected for family member management.");
        }

        return _familyContext.FamilyId.Value;
    }
}
