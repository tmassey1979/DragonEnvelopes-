using System.Text.Json;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class CategorizationRuleEngine(
    IAutomationRuleRepository automationRuleRepository) : ICategorizationRuleEngine
{
    public async Task<string?> EvaluateAsync(
        Guid familyId,
        string description,
        string merchant,
        decimal amount,
        string? currentCategory,
        CancellationToken cancellationToken = default)
    {
        var rules = await automationRuleRepository.ListAsync(
            familyId,
            AutomationRuleType.Categorization,
            true,
            cancellationToken);

        foreach (var rule in rules
                     .OrderBy(x => x.Priority)
                     .ThenBy(x => x.CreatedAt))
        {
            if (!TryParseCondition(rule, out var condition) ||
                !TryParseAction(rule, out var action))
            {
                continue;
            }

            if (Matches(condition, description, merchant, amount, currentCategory))
            {
                return action.SetCategory;
            }
        }

        return null;
    }

    private static bool Matches(
        RuleCondition condition,
        string description,
        string merchant,
        decimal amount,
        string? currentCategory)
    {
        if (!string.IsNullOrWhiteSpace(condition.MerchantContains) &&
            !ContainsIgnoreCase(merchant, condition.MerchantContains))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(condition.DescriptionContains) &&
            !ContainsIgnoreCase(description, condition.DescriptionContains))
        {
            return false;
        }

        if (condition.AmountMin.HasValue && amount < condition.AmountMin.Value)
        {
            return false;
        }

        if (condition.AmountMax.HasValue && amount > condition.AmountMax.Value)
        {
            return false;
        }

        if (condition.CategoryIsNull.HasValue)
        {
            var isNullCategory = string.IsNullOrWhiteSpace(currentCategory);
            if (condition.CategoryIsNull.Value != isNullCategory)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryParseCondition(AutomationRule rule, out RuleCondition condition)
    {
        condition = default;
        try
        {
            using var document = JsonDocument.Parse(rule.ConditionsJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            condition = new RuleCondition(
                GetString(root, "merchantContains"),
                GetString(root, "descriptionContains"),
                GetDecimal(root, "amountMin"),
                GetDecimal(root, "amountMax"),
                GetBool(root, "categoryIsNull"));
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryParseAction(AutomationRule rule, out RuleAction action)
    {
        action = default;
        try
        {
            using var document = JsonDocument.Parse(rule.ActionJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var setCategory = GetString(root, "setCategory");
            if (string.IsNullOrWhiteSpace(setCategory))
            {
                return false;
            }

            action = new RuleAction(setCategory.Trim());
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? GetString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return value.GetString();
    }

    private static decimal? GetDecimal(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return value.GetDecimal();
    }

    private static bool? GetBool(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False)
        {
            return null;
        }

        return value.GetBoolean();
    }

    private static bool ContainsIgnoreCase(string source, string token)
    {
        return source.Contains(token, StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct RuleCondition(
        string? MerchantContains,
        string? DescriptionContains,
        decimal? AmountMin,
        decimal? AmountMax,
        bool? CategoryIsNull);

    private readonly record struct RuleAction(string SetCategory);
}
