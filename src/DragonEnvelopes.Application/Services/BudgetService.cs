using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class BudgetService(
    IBudgetRepository budgetRepository,
    IEnvelopeRepository envelopeRepository,
    IRemainingBudgetCalculator remainingBudgetCalculator) : IBudgetService
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
        return await MapAsync(budget, cancellationToken);
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
        await budgetRepository.SaveChangesAsync(cancellationToken);
        return await MapAsync(budget, cancellationToken);
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
