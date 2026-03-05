namespace DragonEnvelopes.Contracts.Automation;

public sealed record AutomationRuleResponse(
    Guid Id,
    Guid FamilyId,
    string Name,
    string RuleType,
    int Priority,
    bool IsEnabled,
    string ConditionsJson,
    string ActionJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
