namespace DragonEnvelopes.Contracts.Automation;

public sealed record CreateAutomationRuleRequest(
    Guid FamilyId,
    string Name,
    string RuleType,
    int Priority,
    bool IsEnabled,
    string ConditionsJson,
    string ActionJson);
