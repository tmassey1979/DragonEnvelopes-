namespace DragonEnvelopes.Api.Services;

public interface IKeycloakProvisioningService
{
    Task<string> CreateUserAsync(
        string email,
        string displayName,
        string password,
        CancellationToken cancellationToken = default);

    Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default);
}
