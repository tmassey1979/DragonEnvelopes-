namespace DragonEnvelopes.Contracts.Automation;

public sealed record UpdateAutomationRuleRequest(
    string Name,
    int Priority,
    bool IsEnabled,
    string ConditionsJson,
    string ActionJson);
