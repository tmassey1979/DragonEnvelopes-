namespace DragonEnvelopes.Api.Services;

public interface IKeycloakProvisioningService
{
    Task<string> CreateUserAsync(
        string email,
        string firstName,
        string lastName,
        string password,
        CancellationToken cancellationToken = default);

    Task AssignRealmRoleAsync(
        string userId,
        string roleName,
        CancellationToken cancellationToken = default);

    Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default);
}
