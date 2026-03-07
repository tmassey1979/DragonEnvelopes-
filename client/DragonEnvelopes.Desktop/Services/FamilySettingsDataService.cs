using System.Net.Http;
using System.Net.Http.Json;
using DragonEnvelopes.Contracts.Families;
using DragonEnvelopes.Desktop.Api;

namespace DragonEnvelopes.Desktop.Services;

public sealed class FamilySettingsDataService(
    IBackendApiClient apiClient,
    IFamilyContext familyContext) : IFamilySettingsDataService
{
    public async Task<FamilyProfileData> GetFamilyProfileAsync(CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        using var response = await apiClient.GetAsync($"families/{familyId}/profile", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Family profile request failed with status {(int)response.StatusCode}.");
        }

        var payload = await response.Content.ReadFromJsonAsync<FamilyProfileResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Family profile response payload was invalid.");
        return Map(payload);
    }

    public async Task<FamilyProfileData> UpdateFamilyProfileAsync(
        string name,
        string currencyCode,
        string timeZoneId,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new UpdateFamilyProfileRequest(name, currencyCode, timeZoneId);
        using var request = new HttpRequestMessage(HttpMethod.Put, $"families/{familyId}/profile")
        {
            Content = JsonContent.Create(payload)
        };

        using var response = await apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Family profile update failed with status {(int)response.StatusCode}.");
        }

        var updated = await response.Content.ReadFromJsonAsync<FamilyProfileResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Family profile update response payload was invalid.");
        return Map(updated);
    }

    public async Task<FamilyBudgetPreferencesData> GetBudgetPreferencesAsync(CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        using var response = await apiClient.GetAsync($"families/{familyId}/budget-preferences", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Budget preferences request failed with status {(int)response.StatusCode}.");
        }

        var payload = await response.Content.ReadFromJsonAsync<FamilyBudgetPreferencesResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Budget preferences response payload was invalid.");
        return Map(payload);
    }

    public async Task<FamilyBudgetPreferencesData> UpdateBudgetPreferencesAsync(
        string payFrequency,
        string budgetingStyle,
        decimal? householdMonthlyIncome,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new UpdateFamilyBudgetPreferencesRequest(
            payFrequency,
            budgetingStyle,
            householdMonthlyIncome);
        using var request = new HttpRequestMessage(HttpMethod.Put, $"families/{familyId}/budget-preferences")
        {
            Content = JsonContent.Create(payload)
        };

        using var response = await apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Budget preferences update failed with status {(int)response.StatusCode}.");
        }

        var updated = await response.Content.ReadFromJsonAsync<FamilyBudgetPreferencesResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Budget preferences update response payload was invalid.");
        return Map(updated);
    }

    private Guid RequireFamilyId()
    {
        if (!familyContext.FamilyId.HasValue)
        {
            throw new InvalidOperationException("No family selected for settings management.");
        }

        return familyContext.FamilyId.Value;
    }

    private static FamilyProfileData Map(FamilyProfileResponse payload)
    {
        return new FamilyProfileData(
            payload.Id,
            payload.Name,
            payload.CurrencyCode,
            payload.TimeZoneId,
            payload.CreatedAt,
            payload.UpdatedAt);
    }

    private static FamilyBudgetPreferencesData Map(FamilyBudgetPreferencesResponse payload)
    {
        return new FamilyBudgetPreferencesData(
            payload.FamilyId,
            payload.PayFrequency,
            payload.BudgetingStyle,
            payload.HouseholdMonthlyIncome,
            payload.UpdatedAt);
    }
}
