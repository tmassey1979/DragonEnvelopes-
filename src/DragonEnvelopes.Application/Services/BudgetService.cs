using System.Diagnostics;
using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class BudgetService(
    IBudgetRepository budgetRepository,
    IEnvelopeRepository envelopeRepository,
    IRemainingBudgetCalculator remainingBudgetCalculator,
    IIntegrationOutboxRepository? integrationOutboxRepository = null) : IBudgetService
{
    public async Task<BudgetDetails> CreateAsync(
        Guid familyId,
        string month,
        decimal totalIncome,
        CancellationToken cancellationToken = default)
    {
        if (!await budgetRepository.FamilyExistsAsync(familyId, cancellationToken))
        {
            throw new DomainValidationException("Family was not found.");
        }

        var parsedMonth = BudgetMonth.Parse(month);
        if (await budgetRepository.ExistsForMonthAsync(familyId, parsedMonth, cancellationToken))
        {
            throw new DomainValidationException("A budget for this family and month already exists.");
        }

        var budget = new Budget(
            Guid.NewGuid(),
            familyId,
            parsedMonth,
            Money.FromDecimal(totalIncome).EnsureNonNegative("TotalIncome"));

        await budgetRepository.AddAsync(budget, cancellationToken);
        var details = await MapAsync(budget, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        await EnqueuePlanningOutboxAsync(
            familyId,
            IntegrationEventRoutingKeys.PlanningBudgetCreatedV1,
            PlanningIntegrationEventNames.BudgetCreated,
            new BudgetCreatedIntegrationEvent(
                Guid.NewGuid(),
                now,
                familyId,
                ResolveCorrelationId(),
                budget.Id,
                details.Month,
                details.TotalIncome,
                details.AllocatedAmount,
                details.RemainingAmount),
            now,
            cancellationToken);
        await budgetRepository.SaveChangesAsync(cancellationToken);
        return details;
    }

    public async Task<BudgetDetails?> GetByMonthAsync(
        Guid familyId,
        string month,
        CancellationToken cancellationToken = default)
    {
        var parsedMonth = BudgetMonth.Parse(month);
        var budget = await budgetRepository.GetByFamilyAndMonthAsync(familyId, parsedMonth, cancellationToken);
        return budget is null ? null : await MapAsync(budget, cancellationToken);
    }

    public async Task<BudgetDetails> UpdateAsync(
        Guid budgetId,
        decimal totalIncome,
        CancellationToken cancellationToken = default)
    {
        var budget = await budgetRepository.GetByIdForUpdateAsync(budgetId, cancellationToken);
        if (budget is null)
        {
            throw new DomainValidationException("Budget was not found.");
        }

        budget.SetTotalIncome(Money.FromDecimal(totalIncome).EnsureNonNegative("TotalIncome"));
        var details = await MapAsync(budget, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        await EnqueuePlanningOutboxAsync(
            budget.FamilyId,
            IntegrationEventRoutingKeys.PlanningBudgetUpdatedV1,
            PlanningIntegrationEventNames.BudgetUpdated,
            new BudgetUpdatedIntegrationEvent(
                Guid.NewGuid(),
                now,
                budget.FamilyId,
                ResolveCorrelationId(),
                budget.Id,
                details.Month,
                details.TotalIncome,
                details.AllocatedAmount,
                details.RemainingAmount),
            now,
            cancellationToken);
        await budgetRepository.SaveChangesAsync(cancellationToken);
        return details;
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

    private async Task<BudgetDetails> MapAsync(Budget budget, CancellationToken cancellationToken)
    {
        var activeEnvelopes = await envelopeRepository.ListByFamilyAsync(budget.FamilyId, cancellationToken);
        var allocationInputs = activeEnvelopes
            .Where(static envelope => !envelope.IsArchived)
            .Select(static envelope => envelope.MonthlyBudget.Amount)
            .ToArray();
        var remaining = remainingBudgetCalculator.Calculate(budget.TotalIncome.Amount, allocationInputs);

        return new BudgetDetails(
            budget.Id,
            budget.FamilyId,
            budget.Month.ToString(),
            remaining.TotalIncome,
            remaining.AllocatedAmount,
            remaining.RemainingAmount);
    }
}
