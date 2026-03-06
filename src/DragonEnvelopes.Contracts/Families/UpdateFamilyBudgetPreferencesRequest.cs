namespace DragonEnvelopes.Contracts.Families;

public sealed record UpdateFamilyBudgetPreferencesRequest(
    string PayFrequency,
    string BudgetingStyle,
    decimal? HouseholdMonthlyIncome);
