namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record AutomationRuleListItemViewModel(
    Guid Id,
    string Name,
    string RuleType,
    int Priority,
    bool IsEnabled,
    string ConditionsJson,
    string ActionJson,
    string UpdatedAtDisplay);
