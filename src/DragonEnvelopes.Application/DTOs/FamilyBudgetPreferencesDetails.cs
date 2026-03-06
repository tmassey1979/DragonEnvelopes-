namespace DragonEnvelopes.Application.DTOs;

public sealed record FamilyBudgetPreferencesDetails(
    Guid FamilyId,
    string? PayFrequency,
    string? BudgetingStyle,
    decimal? HouseholdMonthlyIncome,
    DateTimeOffset UpdatedAt);
