using System.Diagnostics;
using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class EnvelopeService(
    IEnvelopeRepository envelopeRepository,
    IIntegrationOutboxRepository? integrationOutboxRepository = null) : IEnvelopeService
{
    public async Task<EnvelopeDetails> CreateAsync(
        Guid familyId,
        string name,
        decimal monthlyBudget,
        string? rolloverMode = null,
        decimal? rolloverCap = null,
        CancellationToken cancellationToken = default)
    {
        if (!await envelopeRepository.FamilyExistsAsync(familyId, cancellationToken))
        {
            throw new DomainValidationException("Family was not found.");
        }

        var normalizedName = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
        if (await envelopeRepository.EnvelopeNameExistsAsync(familyId, normalizedName, cancellationToken: cancellationToken))
        {
            throw new DomainValidationException("An envelope with the same name already exists.");
        }

        var envelope = new Envelope(
            Guid.NewGuid(),
            familyId,
            normalizedName,
            Money.FromDecimal(monthlyBudget).EnsureNonNegative("MonthlyBudget"),
            Money.Zero,
            ResolveRolloverMode(rolloverMode, EnvelopeRolloverMode.Full),
            ParseRolloverCap(rolloverCap));

        await envelopeRepository.AddEnvelopeAsync(envelope, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        await EnqueuePlanningOutboxAsync(
            familyId,
            IntegrationEventRoutingKeys.PlanningEnvelopeCreatedV1,
            PlanningIntegrationEventNames.EnvelopeCreated,
            new EnvelopeCreatedIntegrationEvent(
                Guid.NewGuid(),
                now,
                familyId,
                ResolveCorrelationId(),
                envelope.Id,
                envelope.Name,
                envelope.MonthlyBudget.Amount,
                envelope.CurrentBalance.Amount,
                envelope.RolloverMode.ToString(),
                envelope.RolloverCap?.Amount,
                envelope.IsArchived),
            now,
            cancellationToken);
        await envelopeRepository.SaveChangesAsync(cancellationToken);
        return Map(envelope);
    }

    public async Task<EnvelopeDetails?> GetByIdAsync(Guid envelopeId, CancellationToken cancellationToken = default)
    {
        var envelope = await envelopeRepository.GetByIdAsync(envelopeId, cancellationToken);
        return envelope is null ? null : Map(envelope);
    }

    public async Task<IReadOnlyList<EnvelopeDetails>> ListByFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        var items = await envelopeRepository.ListByFamilyAsync(familyId, cancellationToken);
        return items.Select(Map).ToArray();
    }

    public async Task<EnvelopeDetails> UpdateAsync(
        Guid envelopeId,
        string name,
        decimal monthlyBudget,
        bool isArchived,
        string? rolloverMode = null,
        decimal? rolloverCap = null,
        CancellationToken cancellationToken = default)
    {
        var envelope = await envelopeRepository.GetByIdForUpdateAsync(envelopeId, cancellationToken);
        if (envelope is null)
        {
            throw new DomainValidationException("Envelope was not found.");
        }

        var wasArchived = envelope.IsArchived;
        var hasChanges = false;
        var normalizedName = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
        if (!string.Equals(envelope.Name, normalizedName, StringComparison.OrdinalIgnoreCase)
            && await envelopeRepository.EnvelopeNameExistsAsync(
                envelope.FamilyId,
                normalizedName,
                envelope.Id,
                cancellationToken))
        {
            throw new DomainValidationException("An envelope with the same name already exists.");
        }

        if (!string.Equals(envelope.Name, normalizedName, StringComparison.Ordinal))
        {
            envelope.Rename(normalizedName);
            hasChanges = true;
        }

        var budget = Money.FromDecimal(monthlyBudget).EnsureNonNegative("MonthlyBudget");
        if (envelope.MonthlyBudget != budget)
        {
            envelope.SetMonthlyBudget(budget);
            hasChanges = true;
        }

        if (isArchived && !envelope.IsArchived)
        {
            envelope.Archive();
            hasChanges = true;
        }
        else if (!isArchived)
        {
            var parsedRolloverMode = ResolveRolloverMode(rolloverMode, envelope.RolloverMode);
            var parsedRolloverCap = ResolveRolloverCap(parsedRolloverMode, rolloverCap, envelope.RolloverCap);
            if (envelope.RolloverMode != parsedRolloverMode
                || envelope.RolloverCap?.Amount != parsedRolloverCap?.Amount)
            {
                envelope.UpdateRolloverPolicy(parsedRolloverMode, parsedRolloverCap);
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            var now = DateTimeOffset.UtcNow;
            if (!wasArchived && envelope.IsArchived)
            {
                await EnqueuePlanningOutboxAsync(
                    envelope.FamilyId,
                    IntegrationEventRoutingKeys.PlanningEnvelopeArchivedV1,
                    PlanningIntegrationEventNames.EnvelopeArchived,
                    new EnvelopeArchivedIntegrationEvent(
                        Guid.NewGuid(),
                        now,
                        envelope.FamilyId,
                        ResolveCorrelationId(),
                        envelope.Id,
                        envelope.Name,
                        envelope.CurrentBalance.Amount),
                    now,
                    cancellationToken);
            }
            else
            {
                await EnqueuePlanningOutboxAsync(
                    envelope.FamilyId,
                    IntegrationEventRoutingKeys.PlanningEnvelopeUpdatedV1,
                    PlanningIntegrationEventNames.EnvelopeUpdated,
                    new EnvelopeUpdatedIntegrationEvent(
                        Guid.NewGuid(),
                        now,
                        envelope.FamilyId,
                        ResolveCorrelationId(),
                        envelope.Id,
                        envelope.Name,
                        envelope.MonthlyBudget.Amount,
                        envelope.CurrentBalance.Amount,
                        envelope.RolloverMode.ToString(),
                        envelope.RolloverCap?.Amount,
                        envelope.IsArchived),
                    now,
                    cancellationToken);
            }
        }

        await envelopeRepository.SaveChangesAsync(cancellationToken);
        return Map(envelope);
    }

    public async Task<EnvelopeDetails> ArchiveAsync(Guid envelopeId, CancellationToken cancellationToken = default)
    {
        var envelope = await envelopeRepository.GetByIdForUpdateAsync(envelopeId, cancellationToken);
        if (envelope is null)
        {
            throw new DomainValidationException("Envelope was not found.");
        }

        if (!envelope.IsArchived)
        {
            envelope.Archive();
            var now = DateTimeOffset.UtcNow;
            await EnqueuePlanningOutboxAsync(
                envelope.FamilyId,
                IntegrationEventRoutingKeys.PlanningEnvelopeArchivedV1,
                PlanningIntegrationEventNames.EnvelopeArchived,
                new EnvelopeArchivedIntegrationEvent(
                    Guid.NewGuid(),
                    now,
                    envelope.FamilyId,
                    ResolveCorrelationId(),
                    envelope.Id,
                    envelope.Name,
                    envelope.CurrentBalance.Amount),
                now,
                cancellationToken);
            await envelopeRepository.SaveChangesAsync(cancellationToken);
        }

        return Map(envelope);
    }

    public async Task<EnvelopeDetails> UpdateRolloverPolicyAsync(
        Guid envelopeId,
        string rolloverMode,
        decimal? rolloverCap,
        CancellationToken cancellationToken = default)
    {
        var envelope = await envelopeRepository.GetByIdForUpdateAsync(envelopeId, cancellationToken);
        if (envelope is null)
        {
            throw new DomainValidationException("Envelope was not found.");
        }

        envelope.UpdateRolloverPolicy(
            ParseRolloverMode(rolloverMode),
            ParseRolloverCap(rolloverCap));
        var now = DateTimeOffset.UtcNow;
        await EnqueuePlanningOutboxAsync(
            envelope.FamilyId,
            IntegrationEventRoutingKeys.PlanningEnvelopeRolloverPolicyUpdatedV1,
            PlanningIntegrationEventNames.EnvelopeRolloverPolicyUpdated,
            new EnvelopeRolloverPolicyUpdatedIntegrationEvent(
                Guid.NewGuid(),
                now,
                envelope.FamilyId,
                ResolveCorrelationId(),
                envelope.Id,
                envelope.RolloverMode.ToString(),
                envelope.RolloverCap?.Amount),
            now,
            cancellationToken);
        await envelopeRepository.SaveChangesAsync(cancellationToken);
        return Map(envelope);
    }

    private static string ResolveCorrelationId()
    {
        return Activity.Current?.Id ?? Guid.NewGuid().ToString("D");
    }

    private Task EnqueuePlanningOutboxAsync<TPayload>(
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
            IntegrationEventSourceServices.PlanningApi,
            routingKey,
            eventName,
            payload,
            createdAtUtc,
            cancellationToken);
    }

    private static EnvelopeDetails Map(Envelope envelope)
    {
        return new EnvelopeDetails(
            envelope.Id,
            envelope.FamilyId,
            envelope.Name,
            envelope.MonthlyBudget.Amount,
            envelope.CurrentBalance.Amount,
            envelope.RolloverMode.ToString(),
            envelope.RolloverCap?.Amount,
            envelope.LastActivityAt,
            envelope.IsArchived);
    }

    private static EnvelopeRolloverMode ParseRolloverMode(string mode)
    {
        if (!Enum.TryParse<EnvelopeRolloverMode>(mode, ignoreCase: true, out var parsedMode)
            || !Enum.IsDefined(parsedMode))
        {
            throw new DomainValidationException("Rollover mode is invalid.");
        }

        return parsedMode;
    }

    private static EnvelopeRolloverMode ResolveRolloverMode(string? mode, EnvelopeRolloverMode fallback)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            return fallback;
        }

        return ParseRolloverMode(mode);
    }

    private static Money? ParseRolloverCap(decimal? rolloverCap)
    {
        return rolloverCap.HasValue
            ? Money.FromDecimal(rolloverCap.Value).EnsureNonNegative("RolloverCap")
            : null;
    }

    private static Money? ResolveRolloverCap(
        EnvelopeRolloverMode mode,
        decimal? requestedCap,
        Money? fallbackCap)
    {
        if (mode != EnvelopeRolloverMode.Cap)
        {
            return null;
        }

        return requestedCap.HasValue
            ? ParseRolloverCap(requestedCap)
            : fallbackCap;
    }
}
