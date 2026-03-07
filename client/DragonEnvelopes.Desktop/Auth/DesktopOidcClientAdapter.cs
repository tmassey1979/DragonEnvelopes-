using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Results;

namespace DragonEnvelopes.Desktop.Auth;

public sealed class DesktopOidcClientAdapter(OidcClient oidcClient) : IDesktopOidcClient
{
    public Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        return oidcClient.LoginAsync(request, cancellationToken);
    }

    public Task<RefreshTokenResult> RefreshTokenAsync(string refreshToken)
    {
        return oidcClient.RefreshTokenAsync(refreshToken);
    }
}
