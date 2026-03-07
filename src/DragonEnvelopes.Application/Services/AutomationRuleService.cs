using System.Diagnostics;
using DragonEnvelopes.Application.Cqrs.Messaging;
using System.Text.Json;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class AutomationRuleService(
    IAutomationRuleRepository repository,
    IClock clock,
    IIntegrationOutboxRepository? integrationOutboxRepository = null) : IAutomationRuleService
{
    public async Task<AutomationRuleDetails> CreateAsync(
        Guid familyId,
        string name,
        string ruleType,
        int priority,
        bool isEnabled,
        string conditionsJson,
        string actionJson,
        CancellationToken cancellationToken = default)
    {
        if (!await repository.FamilyExistsAsync(familyId, cancellationToken))
        {
            throw new DomainValidationException("Family was not found.");
        }

        ValidateJsonObject(conditionsJson, nameof(conditionsJson));
        ValidateJsonObject(actionJson, nameof(actionJson));
        if (!Enum.TryParse<AutomationRuleType>(ruleType, true, out var parsedType))
        {
            throw new DomainValidationException("Rule type is invalid.");
        }

        var now = clock.UtcNow;
        var rule = new AutomationRule(
            Guid.NewGuid(),
            familyId,
            name,
            parsedType,
            priority,
            isEnabled,
            conditionsJson,
            actionJson,
            now,
            now);

        await repository.AddAsync(rule, cancellationToken);
        await EnqueueAutomationOutboxAsync(
            rule.FamilyId,
            IntegrationEventRoutingKeys.AutomationRuleCreatedV1,
            AutomationIntegrationEventNames.AutomationRuleCreated,
            new AutomationRuleCreatedIntegrationEvent(
                Guid.NewGuid(),
                now,
                rule.FamilyId,
                ResolveCorrelationId(),
                rule.Id,
                rule.Name,
                rule.RuleType.ToString(),
                rule.Priority,
                rule.IsEnabled),
            now,
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(rule);
    }

    public async Task<AutomationRuleDetails?> GetByIdAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        var rule = await repository.GetByIdAsync(ruleId, cancellationToken);
        return rule is null ? null : Map(rule);
    }

    public async Task<IReadOnlyList<AutomationRuleDetails>> ListAsync(
        Guid familyId,
        string? ruleType,
        bool? isEnabled,
        CancellationToken cancellationToken = default)
    {
        AutomationRuleType? parsedType = null;
        if (!string.IsNullOrWhiteSpace(ruleType))
        {
            if (!Enum.TryParse<AutomationRuleType>(ruleType, true, out var typed))
            {
                throw new DomainValidationException("Rule type is invalid.");
            }

            parsedType = typed;
        }

        var items = await repository.ListAsync(familyId, parsedType, isEnabled, cancellationToken);
        return items.Select(Map).ToArray();
    }

    public async Task<AutomationRuleDetails> UpdateAsync(
        Guid ruleId,
        string name,
        int priority,
        bool isEnabled,
        string conditionsJson,
        string actionJson,
        CancellationToken cancellationToken = default)
    {
        var rule = await repository.GetByIdForUpdateAsync(ruleId, cancellationToken);
        if (rule is null)
        {
            throw new DomainValidationException("Automation rule was not found.");
        }

        ValidateJsonObject(conditionsJson, nameof(conditionsJson));
        ValidateJsonObject(actionJson, nameof(actionJson));
        rule.Update(name, priority, isEnabled, conditionsJson, actionJson, clock.UtcNow);
        var now = DateTimeOffset.UtcNow;
        await EnqueueAutomationOutboxAsync(
            rule.FamilyId,
            IntegrationEventRoutingKeys.AutomationRuleUpdatedV1,
            AutomationIntegrationEventNames.AutomationRuleUpdated,
            new AutomationRuleUpdatedIntegrationEvent(
                Guid.NewGuid(),
                now,
                rule.FamilyId,
                ResolveCorrelationId(),
                rule.Id,
                rule.Name,
                rule.RuleType.ToString(),
                rule.Priority,
                rule.IsEnabled),
            now,
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(rule);
    }

    public async Task EnableAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        var rule = await repository.GetByIdForUpdateAsync(ruleId, cancellationToken);
        if (rule is null)
        {
            throw new DomainValidationException("Automation rule was not found.");
        }

        rule.Enable(clock.UtcNow);
        var now = DateTimeOffset.UtcNow;
        await EnqueueAutomationOutboxAsync(
            rule.FamilyId,
            IntegrationEventRoutingKeys.AutomationRuleEnabledV1,
            AutomationIntegrationEventNames.AutomationRuleEnabled,
            new AutomationRuleEnabledIntegrationEvent(
                Guid.NewGuid(),
                now,
                rule.FamilyId,
                ResolveCorrelationId(),
                rule.Id,
                rule.Name,
                rule.RuleType.ToString(),
                rule.Priority),
            now,
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task DisableAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        var rule = await repository.GetByIdForUpdateAsync(ruleId, cancellationToken);
        if (rule is null)
        {
            throw new DomainValidationException("Automation rule was not found.");
        }

        rule.Disable(clock.UtcNow);
        var now = DateTimeOffset.UtcNow;
        await EnqueueAutomationOutboxAsync(
            rule.FamilyId,
            IntegrationEventRoutingKeys.AutomationRuleDisabledV1,
            AutomationIntegrationEventNames.AutomationRuleDisabled,
            new AutomationRuleDisabledIntegrationEvent(
                Guid.NewGuid(),
                now,
                rule.FamilyId,
                ResolveCorrelationId(),
                rule.Id,
                rule.Name,
                rule.RuleType.ToString(),
                rule.Priority),
            now,
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        var rule = await repository.GetByIdForUpdateAsync(ruleId, cancellationToken);
        if (rule is null)
        {
            throw new DomainValidationException("Automation rule was not found.");
        }

        await repository.DeleteAsync(rule, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        await EnqueueAutomationOutboxAsync(
            rule.FamilyId,
            IntegrationEventRoutingKeys.AutomationRuleDeletedV1,
            AutomationIntegrationEventNames.AutomationRuleDeleted,
            new AutomationRuleDeletedIntegrationEvent(
                Guid.NewGuid(),
                now,
                rule.FamilyId,
                ResolveCorrelationId(),
                rule.Id,
                rule.Name,
                rule.RuleType.ToString(),
                rule.Priority,
                rule.IsEnabled),
            now,
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private static AutomationRuleDetails Map(AutomationRule rule)
    {
        return new AutomationRuleDetails(
            rule.Id,
            rule.FamilyId,
            rule.Name,
            rule.RuleType.ToString(),
            rule.Priority,
            rule.IsEnabled,
            rule.ConditionsJson,
            rule.ActionJson,
            rule.CreatedAt,
            rule.UpdatedAt);
    }

    private static void ValidateJsonObject(string json, string fieldName)
    {
        try
        {
            using var doc = JsonDocument.Parse(json ?? string.Empty);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new DomainValidationException($"{fieldName} must be a JSON object.");
            }
        }
        catch (JsonException)
        {
            throw new DomainValidationException($"{fieldName} must be valid JSON.");
        }
    }

    private static string ResolveCorrelationId()
    {
        return Activity.Current?.Id ?? Guid.NewGuid().ToString("D");
    }

    private Task EnqueueAutomationOutboxAsync<TPayload>(
        Guid familyId,
        string routingKey,
        string eventName,
        TPayload payload,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken)
    {
        return IntegrationOutboxEnqueuer.EnqueueAsync(
            integrationOutboxRepository,
            familyId,
            IntegrationEventSourceServices.AutomationApi,
            routingKey,
            eventName,
            payload,
            createdAtUtc,
            cancellationToken);
    }
}
