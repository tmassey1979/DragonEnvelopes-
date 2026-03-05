using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;

namespace DragonEnvelopes.Desktop.Auth;

public sealed class DesktopAuthService : IAuthService
{
    private readonly DesktopAuthOptions _options;
    private readonly IAuthSessionStore _sessionStore;
    private readonly OidcClient _oidcClient;

    public DesktopAuthService(
        IAuthSessionStore sessionStore,
        DesktopAuthOptions? options = null)
    {
        _sessionStore = sessionStore;
        _options = options ?? new DesktopAuthOptions();

        var browser = new LoopbackSystemBrowser(_options.RedirectUri);

        var oidcOptions = new OidcClientOptions
        {
            Authority = _options.Authority,
            ClientId = _options.ClientId,
            Scope = _options.Scope,
            RedirectUri = _options.RedirectUri,
            Browser = browser,
            Policy = new Policy
            {
                Discovery = new IdentityModel.Client.DiscoveryPolicy
                {
                    RequireHttps = false
                }
            }
        };

        _oidcClient = new OidcClient(oidcOptions);
    }

    public async Task<AuthSession?> TryRestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        var session = await _sessionStore.LoadAsync(cancellationToken);
        if (session is null)
        {
            return null;
        }

        if (session.IsExpired(DateTimeOffset.UtcNow))
        {
            await _sessionStore.ClearAsync(cancellationToken);
            return null;
        }

        return session;
    }

    public async Task<AuthSignInResult> SignInAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new LoginRequest
            {
                BrowserTimeout = (int)_options.SignInTimeout.TotalSeconds
            };

            var result = await _oidcClient.LoginAsync(request, cancellationToken);
            if (result.IsError)
            {
                var cancelled = string.Equals(result.Error, "UserCancel", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(result.Error, "Timeout", StringComparison.OrdinalIgnoreCase);

                var message = cancelled
                    ? "Sign-in canceled or timed out."
                    : $"Sign-in failed: {result.Error}";

                return new AuthSignInResult(false, cancelled, message);
            }

            var subject = result.User.Identity?.Name
                ?? result.User.Claims.FirstOrDefault(static claim => claim.Type == "preferred_username")?.Value;
            var session = new AuthSession
            {
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                IdentityToken = result.IdentityToken,
                ExpiresAtUtc = result.AccessTokenExpiration.ToUniversalTime(),
                Subject = subject
            };

            await _sessionStore.SaveAsync(session, cancellationToken);
            return new AuthSignInResult(true, false, "Signed in successfully.", session);
        }
        catch (TaskCanceledException)
        {
            return new AuthSignInResult(false, true, "Sign-in canceled.");
        }
        catch (Exception ex)
        {
            return new AuthSignInResult(false, false, $"Sign-in error: {ex.Message}");
        }
    }

    public Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        return _sessionStore.ClearAsync(cancellationToken);
    }

}
