using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class BudgetService(IBudgetRepository budgetRepository) : IBudgetService
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
        return Map(budget);
    }

    public async Task<BudgetDetails?> GetByMonthAsync(
        Guid familyId,
        string month,
        CancellationToken cancellationToken = default)
    {
        var parsedMonth = BudgetMonth.Parse(month);
        var budget = await budgetRepository.GetByFamilyAndMonthAsync(familyId, parsedMonth, cancellationToken);
        return budget is null ? null : Map(budget);
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
        return Map(budget);
    }

    private static BudgetDetails Map(Budget budget)
    {
        return new BudgetDetails(
            budget.Id,
            budget.FamilyId,
            budget.Month.ToString(),
            budget.TotalIncome.Amount,
            budget.AllocatedAmount.Amount,
            budget.RemainingAmount.Amount);
    }
}
