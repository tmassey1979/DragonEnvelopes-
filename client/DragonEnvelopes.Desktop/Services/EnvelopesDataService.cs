using System.Net.Http;
using System.Text;
using System.Text.Json;
using DragonEnvelopes.Contracts.EnvelopeGoals;
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
        using var envelopeResponse = await _apiClient.GetAsync($"envelopes?familyId={familyId}", cancellationToken);
        if (!envelopeResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Envelopes API request failed with status {(int)envelopeResponse.StatusCode}.");
        }

        await using var stream = await envelopeResponse.Content.ReadAsStreamAsync(cancellationToken);
        var envelopes = await JsonSerializer.DeserializeAsync<List<EnvelopeResponse>>(stream, SerializerOptions, cancellationToken)
            ?? [];

        using var goalsResponse = await _apiClient.GetAsync($"envelope-goals?familyId={familyId}", cancellationToken);
        if (!goalsResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Envelope goals API request failed with status {(int)goalsResponse.StatusCode}.");
        }

        await using var goalsStream = await goalsResponse.Content.ReadAsStreamAsync(cancellationToken);
        var goals = await JsonSerializer.DeserializeAsync<List<EnvelopeGoalResponse>>(goalsStream, SerializerOptions, cancellationToken)
            ?? [];

        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        using var projectionResponse = await _apiClient.GetAsync(
            $"envelope-goals/projection?familyId={familyId}&asOf={asOf:yyyy-MM-dd}",
            cancellationToken);
        if (!projectionResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Envelope goal projection request failed with status {(int)projectionResponse.StatusCode}.");
        }

        await using var projectionStream = await projectionResponse.Content.ReadAsStreamAsync(cancellationToken);
        var projections = await JsonSerializer.DeserializeAsync<List<EnvelopeGoalProjectionResponse>>(
            projectionStream,
            SerializerOptions,
            cancellationToken) ?? [];

        var goalByEnvelopeId = goals.ToDictionary(static goal => goal.EnvelopeId);
        var projectionByEnvelopeId = projections.ToDictionary(static projection => projection.EnvelopeId);

        return envelopes
            .Select(envelope => MapEnvelope(
                envelope,
                goalByEnvelopeId.TryGetValue(envelope.Id, out var goal) ? goal : null,
                projectionByEnvelopeId.TryGetValue(envelope.Id, out var projection) ? projection : null,
                asOf))
            .ToArray();
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

    public async Task CreateGoalAsync(
        Guid envelopeId,
        decimal targetAmount,
        DateOnly dueDate,
        string status,
        CancellationToken cancellationToken = default)
    {
        var payload = new CreateEnvelopeGoalRequest(
            RequireFamilyId(),
            envelopeId,
            targetAmount,
            dueDate,
            status);

        using var response = await SendAsync(HttpMethod.Post, "envelope-goals", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Envelope goal create failed with status {(int)response.StatusCode}.");
        }
    }

    public async Task UpdateGoalAsync(
        Guid goalId,
        decimal targetAmount,
        DateOnly dueDate,
        string status,
        CancellationToken cancellationToken = default)
    {
        var payload = new UpdateEnvelopeGoalRequest(targetAmount, dueDate, status);
        using var response = await SendAsync(HttpMethod.Put, $"envelope-goals/{goalId}", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Envelope goal update failed with status {(int)response.StatusCode}.");
        }
    }

    public async Task DeleteGoalAsync(Guid goalId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Delete, $"envelope-goals/{goalId}", payload: null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Envelope goal delete failed with status {(int)response.StatusCode}.");
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

    private static EnvelopeListItemViewModel MapEnvelope(
        EnvelopeResponse envelope,
        EnvelopeGoalResponse? goal = null,
        EnvelopeGoalProjectionResponse? projection = null,
        DateOnly? asOfDate = null)
    {
        var dueStatus = ResolveDueStatus(goal?.DueDate, asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow));

        return new EnvelopeListItemViewModel(
            envelope.Id,
            envelope.Name,
            envelope.MonthlyBudget,
            envelope.CurrentBalance,
            envelope.IsArchived,
            goalId: goal?.Id,
            goalTargetAmount: goal?.TargetAmount,
            goalDueDate: goal?.DueDate,
            goalStatus: goal?.Status,
            goalProgressPercent: projection?.ProgressPercent,
            goalProjectionStatus: projection?.ProjectionStatus,
            goalDueStatus: dueStatus);
    }

    private static string ResolveDueStatus(DateOnly? dueDate, DateOnly asOfDate)
    {
        if (!dueDate.HasValue)
        {
            return "NoGoal";
        }

        if (dueDate.Value < asOfDate)
        {
            return "Overdue";
        }

        if (dueDate.Value <= asOfDate.AddDays(30))
        {
            return "DueSoon";
        }

        return "OnSchedule";
    }
}
