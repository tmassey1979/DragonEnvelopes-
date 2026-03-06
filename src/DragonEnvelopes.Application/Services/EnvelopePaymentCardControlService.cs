using System.Text.Json;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Services;

public sealed class EnvelopePaymentCardControlService(
    IEnvelopeRepository envelopeRepository,
    IEnvelopePaymentCardRepository envelopePaymentCardRepository,
    IEnvelopePaymentCardControlRepository envelopePaymentCardControlRepository,
    IStripeGateway stripeGateway,
    IClock clock) : IEnvelopePaymentCardControlService
{
    private const string Wildcard = "*";

    public async Task<EnvelopePaymentCardControlDetails> UpsertControlsAsync(
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        decimal? dailyLimitAmount,
        IReadOnlyCollection<string>? allowedMerchantCategories,
        IReadOnlyCollection<string>? allowedMerchantNames,
        string? changedBy,
        CancellationToken cancellationToken = default)
    {
        var envelope = await envelopeRepository.GetByIdAsync(envelopeId, cancellationToken)
            ?? throw new DomainValidationException("Envelope was not found.");
        if (envelope.FamilyId != familyId)
        {
            throw new DomainValidationException("Envelope does not belong to the requested family.");
        }

        var card = await envelopePaymentCardRepository.GetByIdForUpdateAsync(cardId, cancellationToken)
            ?? throw new DomainValidationException("Card was not found.");
        if (card.FamilyId != familyId || card.EnvelopeId != envelopeId)
        {
            throw new DomainValidationException("Card does not belong to the requested family/envelope.");
        }

        var normalizedCategories = NormalizeRules(allowedMerchantCategories, maxLength: 64);
        var normalizedMerchantNames = NormalizeRules(allowedMerchantNames, maxLength: 128);
        EnsureValidRules(dailyLimitAmount, normalizedCategories, normalizedMerchantNames);

        await stripeGateway.UpdateCardSpendingControlsAsync(
            card.ProviderCardId,
            dailyLimitAmount,
            normalizedCategories,
            normalizedMerchantNames,
            cancellationToken);

        var existingControl = await envelopePaymentCardControlRepository.GetByCardIdForUpdateAsync(cardId, cancellationToken);
        var now = clock.UtcNow;
        var normalizedCategoriesJson = SerializeRuleList(normalizedCategories);
        var normalizedMerchantNamesJson = SerializeRuleList(normalizedMerchantNames);
        var actor = string.IsNullOrWhiteSpace(changedBy) ? "system" : changedBy.Trim();

        EnvelopePaymentCardControl control;
        string action;
        string? previousState;
        if (existingControl is null)
        {
            control = new EnvelopePaymentCardControl(
                Guid.NewGuid(),
                familyId,
                envelopeId,
                cardId,
                dailyLimitAmount,
                normalizedCategoriesJson,
                normalizedMerchantNamesJson,
                now,
                now);
            await envelopePaymentCardControlRepository.AddAsync(control, cancellationToken);
            action = "Created";
            previousState = null;
        }
        else
        {
            previousState = SerializeSnapshot(existingControl);
            existingControl.Update(
                dailyLimitAmount,
                normalizedCategoriesJson,
                normalizedMerchantNamesJson,
                now);
            control = existingControl;
            action = "Updated";
        }

        var nextState = SerializeSnapshot(control);
        await envelopePaymentCardControlRepository.AddAuditAsync(
            new EnvelopePaymentCardControlAudit(
                Guid.NewGuid(),
                familyId,
                envelopeId,
                cardId,
                action,
                previousState,
                nextState,
                actor,
                now),
            cancellationToken);
        await envelopePaymentCardControlRepository.SaveChangesAsync(cancellationToken);

        return Map(control);
    }

    public async Task<EnvelopePaymentCardControlDetails?> GetByCardAsync(
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        var card = await envelopePaymentCardRepository.GetByIdAsync(cardId, cancellationToken);
        if (card is null || card.FamilyId != familyId || card.EnvelopeId != envelopeId)
        {
            return null;
        }

        var control = await envelopePaymentCardControlRepository.GetByCardIdAsync(cardId, cancellationToken);
        return control is null ? null : Map(control);
    }

    public async Task<IReadOnlyList<EnvelopePaymentCardControlAuditDetails>> ListAuditByCardAsync(
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        var card = await envelopePaymentCardRepository.GetByIdAsync(cardId, cancellationToken)
            ?? throw new DomainValidationException("Card was not found.");
        if (card.FamilyId != familyId || card.EnvelopeId != envelopeId)
        {
            throw new DomainValidationException("Card does not belong to the requested family/envelope.");
        }

        var auditEntries = await envelopePaymentCardControlRepository.ListAuditByCardIdAsync(cardId, cancellationToken);
        return auditEntries
            .Select(static entry => new EnvelopePaymentCardControlAuditDetails(
                entry.Id,
                entry.FamilyId,
                entry.EnvelopeId,
                entry.CardId,
                entry.Action,
                entry.PreviousStateJson,
                entry.NewStateJson,
                entry.ChangedBy,
                entry.ChangedAtUtc))
            .ToArray();
    }

    public async Task<CardSpendEvaluationDetails> EvaluateSpendAsync(
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        string merchantName,
        string? merchantCategory,
        decimal amount,
        decimal spentTodayAmount,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0m)
        {
            throw new DomainValidationException("Authorization amount must be greater than zero.");
        }

        if (spentTodayAmount < 0m)
        {
            throw new DomainValidationException("SpentTodayAmount cannot be negative.");
        }

        var card = await envelopePaymentCardRepository.GetByIdAsync(cardId, cancellationToken)
            ?? throw new DomainValidationException("Card was not found.");
        if (card.FamilyId != familyId || card.EnvelopeId != envelopeId)
        {
            throw new DomainValidationException("Card does not belong to the requested family/envelope.");
        }

        var control = await envelopePaymentCardControlRepository.GetByCardIdAsync(cardId, cancellationToken);
        if (control is null)
        {
            return new CardSpendEvaluationDetails(true, null, null);
        }

        var normalizedMerchant = NormalizeSingle(merchantName, "Merchant name", maxLength: 128);
        var normalizedCategory = string.IsNullOrWhiteSpace(merchantCategory)
            ? null
            : NormalizeSingle(merchantCategory, "Merchant category", maxLength: 64);
        var allowedCategories = DeserializeRuleList(control.AllowedMerchantCategoriesJson);
        var allowedMerchants = DeserializeRuleList(control.AllowedMerchantNamesJson);

        if (allowedMerchants.Count > 0
            && !allowedMerchants.Contains(normalizedMerchant, StringComparer.OrdinalIgnoreCase))
        {
            return new CardSpendEvaluationDetails(false, "MerchantNotAllowed", RemainingDailyLimit(control.DailyLimitAmount, spentTodayAmount));
        }

        if (allowedCategories.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(normalizedCategory)
                || !allowedCategories.Contains(normalizedCategory, StringComparer.OrdinalIgnoreCase))
            {
                return new CardSpendEvaluationDetails(false, "CategoryNotAllowed", RemainingDailyLimit(control.DailyLimitAmount, spentTodayAmount));
            }
        }

        if (control.DailyLimitAmount.HasValue && spentTodayAmount + amount > control.DailyLimitAmount.Value)
        {
            return new CardSpendEvaluationDetails(false, "DailyLimitExceeded", RemainingDailyLimit(control.DailyLimitAmount, spentTodayAmount));
        }

        return new CardSpendEvaluationDetails(
            true,
            null,
            RemainingDailyLimit(control.DailyLimitAmount, spentTodayAmount + amount));
    }

    private static decimal? RemainingDailyLimit(decimal? dailyLimitAmount, decimal spentTodayAmount)
    {
        if (!dailyLimitAmount.HasValue)
        {
            return null;
        }

        var remaining = dailyLimitAmount.Value - spentTodayAmount;
        return remaining < 0m ? 0m : remaining;
    }

    private static IReadOnlyList<string> NormalizeRules(IReadOnlyCollection<string>? values, int maxLength)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        return values
            .Select(value => NormalizeSingle(value, "Rule value", maxLength))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string NormalizeSingle(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainValidationException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static void EnsureValidRules(
        decimal? dailyLimitAmount,
        IReadOnlyList<string> allowedMerchantCategories,
        IReadOnlyList<string> allowedMerchantNames)
    {
        if (dailyLimitAmount.HasValue && dailyLimitAmount.Value < 0m)
        {
            throw new DomainValidationException("Daily limit amount cannot be negative.");
        }

        if (!dailyLimitAmount.HasValue && allowedMerchantCategories.Count == 0 && allowedMerchantNames.Count == 0)
        {
            throw new DomainValidationException("At least one spending control must be provided.");
        }

        if (allowedMerchantCategories.Count > 1
            && allowedMerchantCategories.Contains(Wildcard, StringComparer.OrdinalIgnoreCase))
        {
            throw new DomainValidationException("Wildcard category cannot be combined with specific categories.");
        }

        if (allowedMerchantNames.Count > 1
            && allowedMerchantNames.Contains(Wildcard, StringComparer.OrdinalIgnoreCase))
        {
            throw new DomainValidationException("Wildcard merchant cannot be combined with specific merchants.");
        }
    }

    private static EnvelopePaymentCardControlDetails Map(EnvelopePaymentCardControl control)
    {
        return new EnvelopePaymentCardControlDetails(
            control.Id,
            control.FamilyId,
            control.EnvelopeId,
            control.CardId,
            control.DailyLimitAmount,
            DeserializeRuleList(control.AllowedMerchantCategoriesJson),
            DeserializeRuleList(control.AllowedMerchantNamesJson),
            control.CreatedAtUtc,
            control.UpdatedAtUtc);
    }

    private static IReadOnlyList<string> DeserializeRuleList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<string[]>(json);
            return parsed?.Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string SerializeRuleList(IReadOnlyList<string> rules)
    {
        return rules.Count == 0 ? "[]" : JsonSerializer.Serialize(rules);
    }

    private static string SerializeSnapshot(EnvelopePaymentCardControl control)
    {
        var snapshot = new
        {
            control.DailyLimitAmount,
            AllowedMerchantCategories = DeserializeRuleList(control.AllowedMerchantCategoriesJson),
            AllowedMerchantNames = DeserializeRuleList(control.AllowedMerchantNamesJson)
        };
        return JsonSerializer.Serialize(snapshot);
    }
}
