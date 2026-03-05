using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using DragonEnvelopes.Contracts.Automation;
using DragonEnvelopes.Desktop.Api;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public sealed class AutomationRulesDataService : IAutomationRulesDataService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IBackendApiClient _apiClient;
    private readonly IFamilyContext _familyContext;

    public AutomationRulesDataService(IBackendApiClient apiClient, IFamilyContext familyContext)
    {
        _apiClient = apiClient;
        _familyContext = familyContext;
    }

    public async Task<IReadOnlyList<AutomationRuleListItemViewModel>> GetRulesAsync(
        string? typeFilter,
        bool? enabledFilter,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var query = $"automation/rules?familyId={familyId}";

        if (!string.IsNullOrWhiteSpace(typeFilter))
        {
            query += $"&type={Uri.EscapeDataString(typeFilter)}";
        }

        if (enabledFilter.HasValue)
        {
            query += $"&enabled={enabledFilter.Value.ToString().ToLowerInvariant()}";
        }

        using var response = await _apiClient.GetAsync(query, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Automation rules request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var rules = await JsonSerializer.DeserializeAsync<List<AutomationRuleResponse>>(stream, SerializerOptions, cancellationToken)
            ?? [];

        return rules
            .OrderBy(static rule => rule.Priority)
            .ThenBy(static rule => rule.CreatedAt)
            .Select(MapRule)
            .ToArray();
    }

    public async Task<AutomationRuleListItemViewModel> CreateRuleAsync(
        string name,
        string ruleType,
        int priority,
        bool isEnabled,
        string conditionsJson,
        string actionJson,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new CreateAutomationRuleRequest(
            familyId,
            name,
            ruleType,
            priority,
            isEnabled,
            conditionsJson,
            actionJson);

        using var request = new HttpRequestMessage(HttpMethod.Post, "automation/rules")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };

        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Create automation rule failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var created = await JsonSerializer.DeserializeAsync<AutomationRuleResponse>(stream, SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("Create automation rule returned an empty response.");

        return MapRule(created);
    }

    public async Task<AutomationRuleListItemViewModel> UpdateRuleAsync(
        Guid ruleId,
        string name,
        int priority,
        bool isEnabled,
        string conditionsJson,
        string actionJson,
        CancellationToken cancellationToken = default)
    {
        var payload = new UpdateAutomationRuleRequest(name, priority, isEnabled, conditionsJson, actionJson);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"automation/rules/{ruleId}")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };

        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Update automation rule failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var updated = await JsonSerializer.DeserializeAsync<AutomationRuleResponse>(stream, SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("Update automation rule returned an empty response.");

        return MapRule(updated);
    }

    public async Task SetRuleEnabledAsync(Guid ruleId, bool enabled, CancellationToken cancellationToken = default)
    {
        var endpoint = enabled
            ? $"automation/rules/{ruleId}/enable"
            : $"automation/rules/{ruleId}/disable";

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Set automation rule enabled state failed with status {(int)response.StatusCode}.");
        }
    }

    public async Task DeleteRuleAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"automation/rules/{ruleId}");
        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Delete automation rule failed with status {(int)response.StatusCode}.");
        }
    }

    private Guid RequireFamilyId()
    {
        if (!_familyContext.FamilyId.HasValue)
        {
            throw new InvalidOperationException("No family selected for automation rule management.");
        }

        return _familyContext.FamilyId.Value;
    }

    private static AutomationRuleListItemViewModel MapRule(AutomationRuleResponse rule)
    {
        return new AutomationRuleListItemViewModel(
            rule.Id,
            rule.Name,
            rule.RuleType,
            rule.Priority,
            rule.IsEnabled,
            rule.ConditionsJson,
            rule.ActionJson,
            rule.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
    }
}
