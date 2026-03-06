namespace DragonEnvelopes.Contracts.Families;

public sealed record FamilyBudgetPreferencesResponse(
    Guid FamilyId,
    string? PayFrequency,
    string? BudgetingStyle,
    decimal? HouseholdMonthlyIncome,
    DateTimeOffset UpdatedAt);
