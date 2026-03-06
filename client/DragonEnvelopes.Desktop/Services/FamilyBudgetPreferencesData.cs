namespace DragonEnvelopes.Desktop.Services;

public sealed record FamilyBudgetPreferencesData(
    Guid FamilyId,
    string? PayFrequency,
    string? BudgetingStyle,
    decimal? HouseholdMonthlyIncome,
    DateTimeOffset UpdatedAt);
