using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using IdentityModel.OidcClient.Results;
using System.Net.Http;
using System.Text.Json;

namespace DragonEnvelopes.Desktop.Auth;

public sealed class DesktopAuthService : IAuthService
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(2);
    private readonly DesktopAuthOptions _options;
    private readonly IAuthSessionStore _sessionStore;
    private readonly IDesktopOidcClient _oidcClient;
    private AuthSession? _currentSession;

    public DesktopAuthService(
        IAuthSessionStore sessionStore,
        DesktopAuthOptions? options = null)
        : this(sessionStore, options, oidcClient: null)
    {
    }

    public DesktopAuthService(
        IAuthSessionStore sessionStore,
        DesktopAuthOptions? options,
        IDesktopOidcClient? oidcClient)
    {
        ArgumentNullException.ThrowIfNull(sessionStore);

        _sessionStore = sessionStore;
        _options = options ?? new DesktopAuthOptions();
        _oidcClient = oidcClient ?? CreateDefaultOidcClient(_options);
    }

    private static IDesktopOidcClient CreateDefaultOidcClient(DesktopAuthOptions options)
    {
        var browser = new LoopbackSystemBrowser(options.RedirectUri);

        var oidcOptions = new OidcClientOptions
        {
            Authority = options.Authority,
            ClientId = options.ClientId,
            Scope = options.Scope,
            RedirectUri = options.RedirectUri,
            Browser = browser,
            Policy = new Policy
            {
                Discovery = new IdentityModel.Client.DiscoveryPolicy
                {
                    RequireHttps = false
                }
            }
        };

        return new DesktopOidcClientAdapter(new OidcClient(oidcOptions));
    }

    public async Task<AuthSession?> TryRestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _sessionStore.LoadAsync(cancellationToken);
            if (session is null || string.IsNullOrWhiteSpace(session.AccessToken))
            {
                await SafeSignOutAsync(cancellationToken);
                return null;
            }

            if (session.IsExpired(DateTimeOffset.UtcNow))
            {
                await SafeSignOutAsync(cancellationToken);
                return null;
            }

            _currentSession = session;
            return session;
        }
        catch
        {
            await SafeSignOutAsync(cancellationToken);
            return null;
        }
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

    public async Task<AuthSignInResult> SignInWithPasswordAsync(
        string usernameOrEmail,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
        {
            return new AuthSignInResult(false, false, "Username/email and password are required.");
        }

        try
        {
            using var httpClient = new HttpClient();
            var tokenEndpoint = $"{_options.Authority.TrimEnd('/')}/protocol/openid-connect/token";
            using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("client_id", _options.ClientId),
                    new KeyValuePair<string, string>("scope", _options.Scope),
                    new KeyValuePair<string, string>("username", usernameOrEmail),
                    new KeyValuePair<string, string>("password", password)
                ])
            };

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new AuthSignInResult(false, false, "Invalid credentials or sign-in is not enabled.");
            }

            using var json = JsonDocument.Parse(payload);
            var root = json.RootElement;

            if (!root.TryGetProperty("access_token", out var accessTokenElement))
            {
                return new AuthSignInResult(false, false, "Authentication response is missing access token.");
            }

            var accessToken = accessTokenElement.GetString();
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return new AuthSignInResult(false, false, "Authentication response returned an empty access token.");
            }

            var refreshToken = root.TryGetProperty("refresh_token", out var refreshTokenElement)
                ? refreshTokenElement.GetString()
                : null;
            var idToken = root.TryGetProperty("id_token", out var idTokenElement)
                ? idTokenElement.GetString()
                : null;
            var expiresIn = root.TryGetProperty("expires_in", out var expiresInElement)
                ? expiresInElement.GetInt32()
                : 300;

            var session = new AuthSession
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                IdentityToken = idToken,
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, expiresIn)),
                Subject = usernameOrEmail
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
        try
        {
            var session = await EnsureSessionAsync(forceRefresh, cancellationToken);
            return string.IsNullOrWhiteSpace(session?.AccessToken)
                ? null
                : session.AccessToken;
        }
        catch
        {
            await SafeSignOutAsync(cancellationToken);
            return null;
        }
    }

    private async Task<AuthSession?> EnsureSessionAsync(
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        try
        {
            _currentSession ??= await _sessionStore.LoadAsync(cancellationToken);
            var session = _currentSession;
            if (session is null || string.IsNullOrWhiteSpace(session.AccessToken))
            {
                await SafeSignOutAsync(cancellationToken);
                return null;
            }

            var nowUtc = DateTimeOffset.UtcNow;
            var shouldRefresh = forceRefresh || session.ExpiresAtUtc <= nowUtc.Add(RefreshSkew);
            if (!shouldRefresh)
            {
                return session;
            }

            if (string.IsNullOrWhiteSpace(session.RefreshToken))
            {
                if (session.IsExpired(nowUtc))
                {
                    await SafeSignOutAsync(cancellationToken);
                    return null;
                }

                return session;
            }

            RefreshTokenResult? refreshResult;
            try
            {
                refreshResult = await _oidcClient.RefreshTokenAsync(session.RefreshToken);
            }
            catch
            {
                if (!session.IsExpired(nowUtc))
                {
                    return session;
                }

                await SafeSignOutAsync(cancellationToken);
                return null;
            }

            if (!TryBuildRefreshedSession(refreshResult, session, out var refreshed))
            {
                if (!session.IsExpired(nowUtc))
                {
                    return session;
                }

                await SafeSignOutAsync(cancellationToken);
                return null;
            }

            _currentSession = refreshed;
            await _sessionStore.SaveAsync(refreshed, cancellationToken);
            return refreshed;
        }
        catch
        {
            await SafeSignOutAsync(cancellationToken);
            return null;
        }
    }

    private static bool TryBuildRefreshedSession(
        RefreshTokenResult? refreshResult,
        AuthSession session,
        out AuthSession refreshed)
    {
        refreshed = null!;
        if (refreshResult is null)
        {
            return false;
        }

        try
        {
            if (refreshResult.IsError)
            {
                return false;
            }

            var accessToken = refreshResult.AccessToken;
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return false;
            }

            var expiresInSeconds = refreshResult.ExpiresIn <= 0
                ? 300
                : refreshResult.ExpiresIn;

            refreshed = new AuthSession
            {
                AccessToken = accessToken,
                RefreshToken = string.IsNullOrWhiteSpace(refreshResult.RefreshToken)
                    ? session.RefreshToken
                    : refreshResult.RefreshToken,
                IdentityToken = refreshResult.IdentityToken,
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds),
                Subject = session.Subject
            };

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task SafeSignOutAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SignOutAsync(cancellationToken);
        }
        catch
        {
            _currentSession = null;
        }
    }
}
