namespace DragonEnvelopes.Desktop.Services;

public interface IOnboardingDataService
{
    Task<OnboardingProfileData> GetProfileAsync(CancellationToken cancellationToken = default);

    Task<OnboardingProfileData> UpdateProfileAsync(
        bool accountsCompleted,
        bool envelopesCompleted,
        bool budgetCompleted,
        CancellationToken cancellationToken = default);
}
