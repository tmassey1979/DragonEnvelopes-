using System.Net.Http;
using System.Text;
using System.Text.Json;
using DragonEnvelopes.Contracts.Envelopes;
using DragonEnvelopes.Desktop.Api;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public sealed class EnvelopesDataService : IEnvelopesDataService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IBackendApiClient _apiClient;
    private readonly IFamilyContext _familyContext;

    public EnvelopesDataService(IBackendApiClient apiClient, IFamilyContext familyContext)
    {
        _apiClient = apiClient;
        _familyContext = familyContext;
    }

    public async Task<IReadOnlyList<EnvelopeListItemViewModel>> GetEnvelopesAsync(CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        using var response = await _apiClient.GetAsync($"envelopes?familyId={familyId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Envelopes API request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var envelopes = await JsonSerializer.DeserializeAsync<List<EnvelopeResponse>>(stream, SerializerOptions, cancellationToken)
            ?? [];

        return envelopes.Select(MapEnvelope).ToArray();
    }

    public async Task<EnvelopeListItemViewModel> CreateEnvelopeAsync(
        string name,
        decimal monthlyBudget,
        CancellationToken cancellationToken = default)
    {
        var request = new CreateEnvelopeRequest(RequireFamilyId(), name, monthlyBudget);
        using var response = await SendAsync(HttpMethod.Post, "envelopes", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Envelope create failed with status {(int)response.StatusCode}.");
        }

        var envelope = await DeserializeEnvelopeAsync(response, cancellationToken);
        return MapEnvelope(envelope);
    }

    public async Task<EnvelopeListItemViewModel> UpdateEnvelopeAsync(
        Guid envelopeId,
        string name,
        decimal monthlyBudget,
        bool isArchived,
        CancellationToken cancellationToken = default)
    {
        var request = new UpdateEnvelopeRequest(name, monthlyBudget, isArchived);
        using var response = await SendAsync(HttpMethod.Put, $"envelopes/{envelopeId}", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Envelope update failed with status {(int)response.StatusCode}.");
        }

        var envelope = await DeserializeEnvelopeAsync(response, cancellationToken);
        return MapEnvelope(envelope);
    }

    public async Task<EnvelopeListItemViewModel> ArchiveEnvelopeAsync(Guid envelopeId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Post, $"envelopes/{envelopeId}/archive", payload: null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Envelope archive failed with status {(int)response.StatusCode}.");
        }

        var envelope = await DeserializeEnvelopeAsync(response, cancellationToken);
        return MapEnvelope(envelope);
    }

    private Guid RequireFamilyId()
    {
        if (!_familyContext.FamilyId.HasValue)
        {
            throw new InvalidOperationException("No family is selected for the current session.");
        }

        return _familyContext.FamilyId.Value;
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string relativePath,
        object? payload,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, relativePath);
        if (payload is not null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload, SerializerOptions),
                Encoding.UTF8,
                "application/json");
        }

        return await _apiClient.SendAsync(request, cancellationToken);
    }

    private static async Task<EnvelopeResponse> DeserializeEnvelopeAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var envelope = await JsonSerializer.DeserializeAsync<EnvelopeResponse>(stream, SerializerOptions, cancellationToken);
        return envelope ?? throw new InvalidOperationException("Envelope response payload was invalid.");
    }

    private static EnvelopeListItemViewModel MapEnvelope(EnvelopeResponse envelope)
    {
        return new EnvelopeListItemViewModel(
            envelope.Id,
            envelope.Name,
            envelope.MonthlyBudget,
            envelope.CurrentBalance,
            envelope.IsArchived);
    }
}
