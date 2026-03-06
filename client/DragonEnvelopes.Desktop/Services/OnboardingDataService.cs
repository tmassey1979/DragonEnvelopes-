using System.Net.Http.Json;
using System.Net.Http;
using DragonEnvelopes.Contracts.Onboarding;
using DragonEnvelopes.Desktop.Api;

namespace DragonEnvelopes.Desktop.Services;

public sealed class OnboardingDataService(
    IBackendApiClient apiClient,
    IFamilyContext familyContext) : IOnboardingDataService
{
    public async Task<OnboardingProfileData> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        using var response = await apiClient.GetAsync($"families/{familyId}/onboarding", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Onboarding profile request failed with status {(int)response.StatusCode}.");
        }

        var payload = await response.Content.ReadFromJsonAsync<OnboardingProfileResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Onboarding profile response payload was invalid.");
        return Map(payload);
    }

    public async Task<OnboardingProfileData> UpdateProfileAsync(
        bool accountsCompleted,
        bool envelopesCompleted,
        bool budgetCompleted,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new UpdateOnboardingProfileRequest(accountsCompleted, envelopesCompleted, budgetCompleted);
        using var request = new HttpRequestMessage(HttpMethod.Put, $"families/{familyId}/onboarding")
        {
            Content = JsonContent.Create(payload)
        };

        using var response = await apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Onboarding profile update failed with status {(int)response.StatusCode}.");
        }

        var updated = await response.Content.ReadFromJsonAsync<OnboardingProfileResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Onboarding update response payload was invalid.");
        return Map(updated);
    }

    private Guid RequireFamilyId()
    {
        if (!familyContext.FamilyId.HasValue)
        {
            throw new InvalidOperationException("No family selected for onboarding.");
        }

        return familyContext.FamilyId.Value;
    }

    private static OnboardingProfileData Map(OnboardingProfileResponse response)
    {
        return new OnboardingProfileData(
            response.Id,
            response.FamilyId,
            response.AccountsCompleted,
            response.EnvelopesCompleted,
            response.BudgetCompleted,
            response.IsCompleted,
            response.CreatedAtUtc,
            response.UpdatedAtUtc,
            response.CompletedAtUtc);
    }
}
