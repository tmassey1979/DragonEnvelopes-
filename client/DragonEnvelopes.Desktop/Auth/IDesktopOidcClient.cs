using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Results;

namespace DragonEnvelopes.Desktop.Auth;

public interface IDesktopOidcClient
{
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<RefreshTokenResult> RefreshTokenAsync(string refreshToken);
}
