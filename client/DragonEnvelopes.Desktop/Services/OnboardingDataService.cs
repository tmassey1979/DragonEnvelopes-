using System.Net.Http.Json;
using System.Net.Http;
using DragonEnvelopes.Contracts.Families;
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
        return new FamilyProfileData(
            payload.Id,
            payload.Name,
            payload.CurrencyCode,
            payload.TimeZoneId,
            payload.CreatedAt,
            payload.UpdatedAt);
    }

    public async Task<OnboardingProfileData> UpdateProfileAsync(
        bool membersCompleted,
        bool accountsCompleted,
        bool envelopesCompleted,
        bool budgetCompleted,
        bool plaidCompleted,
        bool stripeAccountsCompleted,
        bool cardsCompleted,
        bool automationCompleted,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new UpdateOnboardingProfileRequest(
            membersCompleted,
            accountsCompleted,
            envelopesCompleted,
            budgetCompleted,
            plaidCompleted,
            stripeAccountsCompleted,
            cardsCompleted,
            automationCompleted);
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

        return new FamilyProfileData(
            updated.Id,
            updated.Name,
            updated.CurrencyCode,
            updated.TimeZoneId,
            updated.CreatedAt,
            updated.UpdatedAt);
    }

    public async Task<OnboardingProfileData> ReconcileProfileAsync(CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"families/{familyId}/onboarding/reconcile");
        using var response = await apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Onboarding reconcile failed with status {(int)response.StatusCode}.");
        }

        var reconciled = await response.Content.ReadFromJsonAsync<OnboardingProfileResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Onboarding reconcile response payload was invalid.");
        return Map(reconciled);
    }

    public async Task<OnboardingBootstrapResultData> BootstrapAsync(
        IReadOnlyList<(string Name, string Type, decimal OpeningBalance)> accounts,
        IReadOnlyList<(string Name, decimal MonthlyBudget)> envelopes,
        (string Month, decimal TotalIncome)? budget,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new OnboardingBootstrapRequest(
            accounts.Select(static x => new OnboardingBootstrapAccountRequest(x.Name, x.Type, x.OpeningBalance)).ToArray(),
            envelopes.Select(static x => new OnboardingBootstrapEnvelopeRequest(x.Name, x.MonthlyBudget)).ToArray(),
            budget.HasValue
                ? new OnboardingBootstrapBudgetRequest(budget.Value.Month, budget.Value.TotalIncome)
                : null);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"families/{familyId}/onboarding/bootstrap")
        {
            Content = JsonContent.Create(payload)
        };

        using var response = await apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Onboarding bootstrap failed with status {(int)response.StatusCode}.");
        }

        var result = await response.Content.ReadFromJsonAsync<OnboardingBootstrapResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Onboarding bootstrap response payload was invalid.");

        return new OnboardingBootstrapResultData(
            result.FamilyId,
            result.AccountsCreated,
            result.EnvelopesCreated,
            result.BudgetCreated);
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
            response.MembersCompleted,
            response.AccountsCompleted,
            response.EnvelopesCompleted,
            response.BudgetCompleted,
            response.PlaidCompleted,
            response.StripeAccountsCompleted,
            response.CardsCompleted,
            response.AutomationCompleted,
            response.IsCompleted,
            response.CreatedAtUtc,
            response.UpdatedAtUtc,
            response.CompletedAtUtc);
    }
}
