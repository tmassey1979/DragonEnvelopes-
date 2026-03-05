using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;

namespace DragonEnvelopes.Desktop.Auth;

public sealed class DesktopAuthService : IAuthService
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(2);
    private readonly DesktopAuthOptions _options;
    private readonly IAuthSessionStore _sessionStore;
    private readonly OidcClient _oidcClient;
    private AuthSession? _currentSession;

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
            _currentSession = null;
            return null;
        }

        if (session.IsExpired(DateTimeOffset.UtcNow))
        {
            await _sessionStore.ClearAsync(cancellationToken);
            _currentSession = null;
            return null;
        }

        _currentSession = session;
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

            _currentSession = session;
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
        _currentSession = null;
        return _sessionStore.ClearAsync(cancellationToken);
    }

    public async Task<string?> GetAccessTokenAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var session = await EnsureSessionAsync(forceRefresh, cancellationToken);
        return session?.AccessToken;
    }

    private async Task<AuthSession?> EnsureSessionAsync(
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        _currentSession ??= await _sessionStore.LoadAsync(cancellationToken);
        if (_currentSession is null)
        {
            return null;
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var shouldRefresh = forceRefresh || _currentSession.ExpiresAtUtc <= nowUtc.Add(RefreshSkew);
        if (!shouldRefresh)
        {
            return _currentSession;
        }

        if (string.IsNullOrWhiteSpace(_currentSession.RefreshToken))
        {
            if (_currentSession.IsExpired(nowUtc))
            {
                await SignOutAsync(cancellationToken);
                return null;
            }

            return _currentSession;
        }

        var refreshResult = await _oidcClient.RefreshTokenAsync(_currentSession.RefreshToken);
        if (refreshResult.IsError)
        {
            await SignOutAsync(cancellationToken);
            return null;
        }

        var refreshed = new AuthSession
        {
            AccessToken = refreshResult.AccessToken,
            RefreshToken = string.IsNullOrWhiteSpace(refreshResult.RefreshToken)
                ? _currentSession.RefreshToken
                : refreshResult.RefreshToken,
            IdentityToken = refreshResult.IdentityToken,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(refreshResult.ExpiresIn),
            Subject = _currentSession.Subject
        };

        _currentSession = refreshed;
        await _sessionStore.SaveAsync(refreshed, cancellationToken);
        return refreshed;
    }
}
