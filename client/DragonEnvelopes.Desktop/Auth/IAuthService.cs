namespace DragonEnvelopes.Desktop.Auth;

public interface IAuthService
{
    Task<AuthSession?> TryRestoreSessionAsync(CancellationToken cancellationToken = default);

    Task<AuthSignInResult> SignInAsync(CancellationToken cancellationToken = default);

    Task SignOutAsync(CancellationToken cancellationToken = default);
}
