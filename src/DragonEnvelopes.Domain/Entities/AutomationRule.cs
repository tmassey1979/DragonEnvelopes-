using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Domain.Entities;

public sealed class AutomationRule
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public string Name { get; private set; }
    public AutomationRuleType RuleType { get; private set; }
    public int Priority { get; private set; }
    public bool IsEnabled { get; private set; }
    public string ConditionsJson { get; private set; }
    public string ActionJson { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public AutomationRule(
        Guid id,
        Guid familyId,
        string name,
        AutomationRuleType ruleType,
        int priority,
        bool isEnabled,
        string conditionsJson,
        string actionJson,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Automation rule id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (priority < 1)
        {
            throw new DomainValidationException("Priority must be greater than or equal to 1.");
        }

        Id = id;
        FamilyId = familyId;
        Name = ValidateRequired(name, "Rule name");
        RuleType = ruleType;
        Priority = priority;
        IsEnabled = isEnabled;
        ConditionsJson = ValidateRequired(conditionsJson, "ConditionsJson");
        ActionJson = ValidateRequired(actionJson, "ActionJson");
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public void Update(
        string name,
        int priority,
        bool isEnabled,
        string conditionsJson,
        string actionJson,
        DateTimeOffset updatedAt)
    {
        if (priority < 1)
        {
            throw new DomainValidationException("Priority must be greater than or equal to 1.");
        }

        Name = ValidateRequired(name, "Rule name");
        Priority = priority;
        IsEnabled = isEnabled;
        ConditionsJson = ValidateRequired(conditionsJson, "ConditionsJson");
        ActionJson = ValidateRequired(actionJson, "ActionJson");
        UpdatedAt = updatedAt;
    }

    public void Enable(DateTimeOffset updatedAt)
    {
        IsEnabled = true;
        UpdatedAt = updatedAt;
    }

    public void Disable(DateTimeOffset updatedAt)
    {
        IsEnabled = false;
        UpdatedAt = updatedAt;
    }

    private static string ValidateRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        return value.Trim();
    }
}
