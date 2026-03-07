namespace DragonEnvelopes.Desktop.Services;

public interface IFamilySettingsDataService
{
    Task<FamilyProfileData> GetFamilyProfileAsync(CancellationToken cancellationToken = default);

    Task<FamilyProfileData> UpdateFamilyProfileAsync(
        string name,
        string currencyCode,
        string timeZoneId,
        CancellationToken cancellationToken = default);

    Task<FamilyBudgetPreferencesData> GetBudgetPreferencesAsync(CancellationToken cancellationToken = default);

    Task<FamilyBudgetPreferencesData> UpdateBudgetPreferencesAsync(
        string payFrequency,
        string budgetingStyle,
        decimal? householdMonthlyIncome,
        CancellationToken cancellationToken = default);
}
