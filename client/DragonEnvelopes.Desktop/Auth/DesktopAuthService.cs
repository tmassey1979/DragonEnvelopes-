using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using System.Net.Http;
using System.Text.Json;

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
        var session = await EnsureSessionAsync(forceRefresh, cancellationToken);
        return session?.AccessToken;
    }

    private async Task<AuthSession?> EnsureSessionAsync(
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        _currentSession ??= await _sessionStore.LoadAsync(cancellationToken);
        var session = _currentSession;
        if (session is null)
        {
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
                await SignOutAsync(cancellationToken);
                return null;
            }

            return session;
        }

        var refreshResult = await _oidcClient.RefreshTokenAsync(session.RefreshToken);
        if (refreshResult is null || refreshResult.IsError || string.IsNullOrWhiteSpace(refreshResult.AccessToken))
        {
            await SignOutAsync(cancellationToken);
            return null;
        }

        var refreshed = new AuthSession
        {
            AccessToken = refreshResult.AccessToken,
            RefreshToken = string.IsNullOrWhiteSpace(refreshResult.RefreshToken)
                ? session.RefreshToken
                : refreshResult.RefreshToken,
            IdentityToken = refreshResult.IdentityToken,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(refreshResult.ExpiresIn),
            Subject = session.Subject
        };

        _currentSession = refreshed;
        await _sessionStore.SaveAsync(refreshed, cancellationToken);
        return refreshed;
    }
}
