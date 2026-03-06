using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class OnboardingBootstrapService(
    IOnboardingBootstrapRepository onboardingBootstrapRepository) : IOnboardingBootstrapService
{
    private static readonly string[] AllowedAccountTypes = ["Checking", "Savings", "Cash", "Credit"];

    public async Task<OnboardingBootstrapDetails> BootstrapAsync(
        Guid familyId,
        IReadOnlyList<(string Name, string Type, decimal OpeningBalance)> accounts,
        IReadOnlyList<(string Name, decimal MonthlyBudget)> envelopes,
        (string Month, decimal TotalIncome)? budget,
        CancellationToken cancellationToken = default)
    {
        if (!await onboardingBootstrapRepository.FamilyExistsAsync(familyId, cancellationToken))
        {
            throw new DomainValidationException("Family was not found.");
        }

        if (accounts.Count == 0 && envelopes.Count == 0 && budget is null)
        {
            throw new DomainValidationException("At least one onboarding item is required.");
        }

        var existingAccountNames = await onboardingBootstrapRepository.ListAccountNamesAsync(familyId, cancellationToken);
        var existingEnvelopeNames = await onboardingBootstrapRepository.ListEnvelopeNamesAsync(familyId, cancellationToken);

        var normalizedAccountNames = new HashSet<string>(existingAccountNames, StringComparer.OrdinalIgnoreCase);
        var normalizedEnvelopeNames = new HashSet<string>(existingEnvelopeNames, StringComparer.OrdinalIgnoreCase);

        var accountEntities = new List<Account>();
        foreach (var account in accounts)
        {
            var name = NormalizeRequired(account.Name, "Account name");
            var type = NormalizeRequired(account.Type, "Account type");
            if (!AllowedAccountTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            {
                throw new DomainValidationException($"Account type '{type}' is invalid.");
            }

            if (!normalizedAccountNames.Add(name))
            {
                throw new DomainValidationException($"Duplicate account name '{name}' detected.");
            }

            if (account.OpeningBalance < 0m)
            {
                throw new DomainValidationException("Opening balance cannot be negative.");
            }

            accountEntities.Add(new Account(
                Guid.NewGuid(),
                familyId,
                name,
                Enum.Parse<AccountType>(type, ignoreCase: true),
                Money.FromDecimal(account.OpeningBalance)));
        }

        var envelopeEntities = new List<Envelope>();
        foreach (var envelope in envelopes)
        {
            var name = NormalizeRequired(envelope.Name, "Envelope name");
            if (!normalizedEnvelopeNames.Add(name))
            {
                throw new DomainValidationException($"Duplicate envelope name '{name}' detected.");
            }

            if (envelope.MonthlyBudget < 0m)
            {
                throw new DomainValidationException("Monthly budget cannot be negative.");
            }

            envelopeEntities.Add(new Envelope(
                Guid.NewGuid(),
                familyId,
                name,
                Money.FromDecimal(envelope.MonthlyBudget),
                Money.FromDecimal(0m)));
        }

        Budget? budgetEntity = null;
        if (budget.HasValue)
        {
            var parsedMonth = BudgetMonth.Parse(budget.Value.Month);
            if (await onboardingBootstrapRepository.BudgetExistsAsync(familyId, parsedMonth, cancellationToken))
            {
                throw new DomainValidationException($"Budget already exists for month '{parsedMonth}'.");
            }

            if (budget.Value.TotalIncome < 0m)
            {
                throw new DomainValidationException("Budget total income cannot be negative.");
            }

            budgetEntity = new Budget(
                Guid.NewGuid(),
                familyId,
                parsedMonth,
                Money.FromDecimal(budget.Value.TotalIncome));
        }

        await onboardingBootstrapRepository.SaveBootstrapAsync(
            accountEntities,
            envelopeEntities,
            budgetEntity,
            cancellationToken);

        return new OnboardingBootstrapDetails(
            familyId,
            accountEntities.Count,
            envelopeEntities.Count,
            budgetEntity is not null);
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        return value.Trim();
    }
}
