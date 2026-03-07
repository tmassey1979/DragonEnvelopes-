using System.Diagnostics;
using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class EnvelopeGoalService(
    IEnvelopeGoalRepository envelopeGoalRepository,
    IEnvelopeRepository envelopeRepository,
    IClock clock,
    IIntegrationOutboxRepository? integrationOutboxRepository = null) : IEnvelopeGoalService
{
    public async Task<EnvelopeGoalDetails> CreateAsync(
        Guid familyId,
        Guid envelopeId,
        decimal targetAmount,
        DateOnly dueDate,
        string status,
        CancellationToken cancellationToken = default)
    {
        if (!await envelopeGoalRepository.FamilyExistsAsync(familyId, cancellationToken))
        {
            throw new DomainValidationException("Family was not found.");
        }

        if (!await envelopeGoalRepository.EnvelopeExistsAsync(envelopeId, familyId, cancellationToken))
        {
            throw new DomainValidationException("Envelope was not found for this family.");
        }

        if (await envelopeGoalRepository.ExistsForEnvelopeAsync(envelopeId, cancellationToken: cancellationToken))
        {
            throw new DomainValidationException("Envelope already has a goal.");
        }

        var now = clock.UtcNow;
        var goal = new EnvelopeGoal(
            Guid.NewGuid(),
            familyId,
            envelopeId,
            Money.FromDecimal(targetAmount),
            dueDate,
            ParseStatus(status),
            now,
            now);

        await envelopeGoalRepository.AddAsync(goal, cancellationToken);
        await EnqueuePlanningOutboxAsync(
            goal.FamilyId,
            IntegrationEventRoutingKeys.PlanningEnvelopeGoalCreatedV1,
            PlanningIntegrationEventNames.EnvelopeGoalCreated,
            new EnvelopeGoalCreatedIntegrationEvent(
                Guid.NewGuid(),
                now,
                goal.FamilyId,
                ResolveCorrelationId(),
                goal.Id,
                goal.EnvelopeId,
                goal.TargetAmount.Amount,
                goal.DueDate,
                goal.Status.ToString()),
            now,
            cancellationToken);
        await envelopeGoalRepository.SaveChangesAsync(cancellationToken);
        return await MapWithEnvelopeAsync(goal, cancellationToken);
    }

    public async Task<EnvelopeGoalDetails?> GetByIdAsync(Guid goalId, CancellationToken cancellationToken = default)
    {
        var goal = await envelopeGoalRepository.GetByIdAsync(goalId, cancellationToken);
        if (goal is null)
        {
            return null;
        }

        return await MapWithEnvelopeAsync(goal, cancellationToken);
    }

    public async Task<IReadOnlyList<EnvelopeGoalDetails>> ListByFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        var goals = await envelopeGoalRepository.ListByFamilyAsync(familyId, cancellationToken);
        if (goals.Count == 0)
        {
            return [];
        }

        var envelopes = await envelopeRepository.ListByFamilyAsync(familyId, cancellationToken);
        var envelopeMap = envelopes.ToDictionary(static envelope => envelope.Id);

        return goals
            .Where(goal => envelopeMap.ContainsKey(goal.EnvelopeId))
            .Select(goal => Map(goal, envelopeMap[goal.EnvelopeId]))
            .OrderBy(static goal => goal.DueDate)
            .ThenBy(static goal => goal.EnvelopeName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<EnvelopeGoalDetails> UpdateAsync(
        Guid goalId,
        decimal targetAmount,
        DateOnly dueDate,
        string status,
        CancellationToken cancellationToken = default)
    {
        var goal = await envelopeGoalRepository.GetByIdForUpdateAsync(goalId, cancellationToken);
        if (goal is null)
        {
            throw new DomainValidationException("Envelope goal was not found.");
        }

        goal.Update(
            Money.FromDecimal(targetAmount),
            dueDate,
            ParseStatus(status),
            clock.UtcNow);
        var now = DateTimeOffset.UtcNow;
        await EnqueuePlanningOutboxAsync(
            goal.FamilyId,
            IntegrationEventRoutingKeys.PlanningEnvelopeGoalUpdatedV1,
            PlanningIntegrationEventNames.EnvelopeGoalUpdated,
            new EnvelopeGoalUpdatedIntegrationEvent(
                Guid.NewGuid(),
                now,
                goal.FamilyId,
                ResolveCorrelationId(),
                goal.Id,
                goal.EnvelopeId,
                goal.TargetAmount.Amount,
                goal.DueDate,
                goal.Status.ToString()),
            now,
            cancellationToken);
        await envelopeGoalRepository.SaveChangesAsync(cancellationToken);
        return await MapWithEnvelopeAsync(goal, cancellationToken);
    }

    public async Task DeleteAsync(Guid goalId, CancellationToken cancellationToken = default)
    {
        var goal = await envelopeGoalRepository.GetByIdForUpdateAsync(goalId, cancellationToken);
        if (goal is null)
        {
            throw new DomainValidationException("Envelope goal was not found.");
        }

        await envelopeGoalRepository.DeleteAsync(goal, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        await EnqueuePlanningOutboxAsync(
            goal.FamilyId,
            IntegrationEventRoutingKeys.PlanningEnvelopeGoalDeletedV1,
            PlanningIntegrationEventNames.EnvelopeGoalDeleted,
            new EnvelopeGoalDeletedIntegrationEvent(
                Guid.NewGuid(),
                now,
                goal.FamilyId,
                ResolveCorrelationId(),
                goal.Id,
                goal.EnvelopeId),
            now,
            cancellationToken);
        await envelopeGoalRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EnvelopeGoalProjectionDetails>> ProjectAsync(
        Guid familyId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default)
    {
        var goals = await envelopeGoalRepository.ListByFamilyAsync(familyId, cancellationToken);
        if (goals.Count == 0)
        {
            return [];
        }

        var envelopes = await envelopeRepository.ListByFamilyAsync(familyId, cancellationToken);
        var envelopeMap = envelopes.ToDictionary(static envelope => envelope.Id);
        var items = new List<EnvelopeGoalProjectionDetails>();

        foreach (var goal in goals)
        {
            if (!envelopeMap.TryGetValue(goal.EnvelopeId, out var envelope))
            {
                continue;
            }

            var targetAmount = goal.TargetAmount.Amount;
            var currentBalance = envelope.CurrentBalance.Amount;
            var progressPercent = targetAmount == 0m
                ? 0m
                : decimal.Round(
                    Math.Min(100m, (currentBalance / targetAmount) * 100m),
                    1,
                    MidpointRounding.AwayFromZero);

            var createdOn = DateOnly.FromDateTime(goal.CreatedAtUtc.UtcDateTime);
            var totalDays = Math.Max(1, goal.DueDate.DayNumber - createdOn.DayNumber);
            var elapsedDays = Math.Clamp(asOfDate.DayNumber - createdOn.DayNumber, 0, totalDays);
            var expectedProgressPercent = decimal.Round(
                (elapsedDays / (decimal)totalDays) * 100m,
                1,
                MidpointRounding.AwayFromZero);
            var expectedBalance = decimal.Round(
                targetAmount * (expectedProgressPercent / 100m),
                2,
                MidpointRounding.AwayFromZero);
            var varianceAmount = decimal.Round(
                currentBalance - expectedBalance,
                2,
                MidpointRounding.AwayFromZero);

            var projectionStatus = asOfDate >= goal.DueDate
                ? currentBalance >= targetAmount ? "OnTrack" : "Behind"
                : currentBalance >= expectedBalance ? "OnTrack" : "Behind";

            items.Add(new EnvelopeGoalProjectionDetails(
                goal.Id,
                goal.FamilyId,
                goal.EnvelopeId,
                envelope.Name,
                currentBalance,
                targetAmount,
                goal.DueDate,
                goal.Status.ToString(),
                progressPercent,
                expectedProgressPercent,
                expectedBalance,
                varianceAmount,
                projectionStatus));
        }

        return items
            .OrderBy(static item => item.DueDate)
            .ThenBy(static item => item.EnvelopeName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<EnvelopeGoalDetails> MapWithEnvelopeAsync(
        EnvelopeGoal goal,
        CancellationToken cancellationToken)
    {
        var envelope = await envelopeRepository.GetByIdAsync(goal.EnvelopeId, cancellationToken);
        if (envelope is null)
        {
            throw new DomainValidationException("Envelope was not found for this goal.");
        }

        return Map(goal, envelope);
    }

    private static EnvelopeGoalDetails Map(EnvelopeGoal goal, Envelope envelope)
    {
        return new EnvelopeGoalDetails(
            goal.Id,
            goal.FamilyId,
            goal.EnvelopeId,
            envelope.Name,
            envelope.CurrentBalance.Amount,
            goal.TargetAmount.Amount,
            goal.DueDate,
            goal.Status.ToString(),
            goal.CreatedAtUtc,
            goal.UpdatedAtUtc);
    }

    private static EnvelopeGoalStatus ParseStatus(string status)
    {
        if (!Enum.TryParse<EnvelopeGoalStatus>(status, true, out var parsedStatus)
            || !Enum.IsDefined(parsedStatus))
        {
            throw new DomainValidationException("Envelope goal status is invalid.");
        }

        return parsedStatus;
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
}
