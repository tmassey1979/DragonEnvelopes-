using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IAutomationRuleService
{
    Task<AutomationRuleDetails> CreateAsync(
        Guid familyId,
        string name,
        string ruleType,
        int priority,
        bool isEnabled,
        string conditionsJson,
        string actionJson,
        CancellationToken cancellationToken = default);

    Task<AutomationRuleDetails?> GetByIdAsync(Guid ruleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AutomationRuleDetails>> ListAsync(
        Guid familyId,
        string? ruleType,
        bool? isEnabled,
        CancellationToken cancellationToken = default);

    Task<AutomationRuleDetails> UpdateAsync(
        Guid ruleId,
        string name,
        int priority,
        bool isEnabled,
        string conditionsJson,
        string actionJson,
        CancellationToken cancellationToken = default);

    Task EnableAsync(Guid ruleId, CancellationToken cancellationToken = default);

    Task DisableAsync(Guid ruleId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid ruleId, CancellationToken cancellationToken = default);
}
