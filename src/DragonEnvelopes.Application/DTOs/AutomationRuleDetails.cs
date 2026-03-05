namespace DragonEnvelopes.Application.DTOs;

public sealed record AutomationRuleDetails(
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
