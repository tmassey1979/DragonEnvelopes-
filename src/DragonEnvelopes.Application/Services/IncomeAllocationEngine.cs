using System.Text.Json;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class IncomeAllocationEngine(
    IAutomationRuleRepository automationRuleRepository) : IIncomeAllocationEngine
{
    public async Task<IReadOnlyList<TransactionSplitCreateDetails>> AllocateAsync(
        Guid familyId,
        string description,
        string merchant,
        decimal amount,
        string? currentCategory,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0m)
        {
            return [];
        }

        var rules = await automationRuleRepository.ListAsync(
            familyId,
            AutomationRuleType.Allocation,
            true,
            cancellationToken);

        var splits = new List<TransactionSplitCreateDetails>();
        var remaining = amount;

        foreach (var rule in rules
                     .OrderBy(x => x.Priority)
                     .ThenBy(x => x.CreatedAt))
        {
            if (remaining <= 0m)
            {
                break;
            }

            if (!TryParseCondition(rule, out var condition) ||
                !TryParseAction(rule, out var action))
            {
                continue;
            }

            if (!Matches(condition, description, merchant, amount, currentCategory))
            {
                continue;
            }

            var requested = action.AllocationType switch
            {
                AllocationType.FixedAmount => action.Value,
                AllocationType.Percent => decimal.Round(amount * (action.Value / 100m), 2, MidpointRounding.AwayFromZero),
                _ => 0m
            };

            if (requested <= 0m)
            {
                continue;
            }

            var allocated = decimal.Min(requested, remaining);
            if (allocated <= 0m)
            {
                continue;
            }

            splits.Add(new TransactionSplitCreateDetails(
                action.TargetEnvelopeId,
                allocated,
                currentCategory,
                "Auto allocation"));
            remaining -= allocated;
        }

        return splits;
    }

    private static bool Matches(
        RuleCondition condition,
        string description,
        string merchant,
        decimal amount,
        string? currentCategory)
    {
        if (!string.IsNullOrWhiteSpace(condition.MerchantContains) &&
            !merchant.Contains(condition.MerchantContains, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(condition.DescriptionContains) &&
            !description.Contains(condition.DescriptionContains, StringComparison.OrdinalIgnoreCase))
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

            var envelopeIdText = GetString(root, "targetEnvelopeId");
            var allocationTypeText = GetString(root, "allocationType");
            var value = GetDecimal(root, "value");
            if (!Guid.TryParse(envelopeIdText, out var envelopeId))
            {
                return false;
            }

            if (!Enum.TryParse<AllocationType>(allocationTypeText, true, out var allocationType))
            {
                return false;
            }

            if (!value.HasValue || value.Value <= 0m)
            {
                return false;
            }

            action = new RuleAction(envelopeId, allocationType, value.Value);
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
        if (!root.TryGetProperty(propertyName, out var value) ||
            (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False))
        {
            return null;
        }

        return value.GetBoolean();
    }

    private readonly record struct RuleCondition(
        string? MerchantContains,
        string? DescriptionContains,
        decimal? AmountMin,
        decimal? AmountMax,
        bool? CategoryIsNull);

    private readonly record struct RuleAction(
        Guid TargetEnvelopeId,
        AllocationType AllocationType,
        decimal Value);

    private enum AllocationType
    {
        FixedAmount,
        Percent
    }
}
