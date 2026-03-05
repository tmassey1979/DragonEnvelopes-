using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public interface IAutomationRulesDataService
{
    Task<IReadOnlyList<AutomationRuleListItemViewModel>> GetRulesAsync(
        string? typeFilter,
        bool? enabledFilter,
        CancellationToken cancellationToken = default);

    Task<AutomationRuleListItemViewModel> CreateRuleAsync(
        string name,
        string ruleType,
        int priority,
        bool isEnabled,
        string conditionsJson,
        string actionJson,
        CancellationToken cancellationToken = default);

    Task<AutomationRuleListItemViewModel> UpdateRuleAsync(
        Guid ruleId,
        string name,
        int priority,
        bool isEnabled,
        string conditionsJson,
        string actionJson,
        CancellationToken cancellationToken = default);

    Task SetRuleEnabledAsync(Guid ruleId, bool enabled, CancellationToken cancellationToken = default);

    Task DeleteRuleAsync(Guid ruleId, CancellationToken cancellationToken = default);
}
