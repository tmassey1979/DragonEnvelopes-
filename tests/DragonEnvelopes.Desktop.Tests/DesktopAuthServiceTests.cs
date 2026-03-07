using DragonEnvelopes.Desktop.Auth;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Results;

namespace DragonEnvelopes.Desktop.Tests;

public sealed class DesktopAuthServiceTests
{
    [Fact]
    public async Task TryRestoreSessionAsync_ReturnsNull_AndClears_WhenStoredSessionHasNoAccessToken()
    {
        var store = new TestSessionStore
        {
            LoadResult = new AuthSession
            {
                AccessToken = string.Empty,
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(20)
            }
        };

        var service = new DesktopAuthService(store, new DesktopAuthOptions(), new TestOidcClient());

        var restored = await service.TryRestoreSessionAsync();

        Assert.Null(restored);
        Assert.Equal(1, store.ClearCallCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ReturnsCurrentToken_WhenRefreshThrows_AndSessionStillValid()
    {
        var store = new TestSessionStore
        {
            LoadResult = new AuthSession
            {
                AccessToken = "existing-token",
                RefreshToken = "refresh-token",
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(20),
                Subject = "user-a"
            }
        };
        var oidc = new TestOidcClient
        {
            RefreshException = new NullReferenceException("Simulated malformed refresh response.")
        };
        var service = new DesktopAuthService(store, new DesktopAuthOptions(), oidc);

        var accessToken = await service.GetAccessTokenAsync(forceRefresh: true);

        Assert.Equal("existing-token", accessToken);
        Assert.Equal(0, store.ClearCallCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ReturnsNull_AndClears_WhenRefreshResultIsNull_ForExpiredSession()
    {
        var store = new TestSessionStore
        {
            LoadResult = new AuthSession
            {
                AccessToken = "expired-token",
                RefreshToken = "refresh-token",
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
                Subject = "user-a"
            }
        };
        var oidc = new TestOidcClient
        {
            RefreshResult = null
        };
        var service = new DesktopAuthService(store, new DesktopAuthOptions(), oidc);

        var accessToken = await service.GetAccessTokenAsync(forceRefresh: true);

        Assert.Null(accessToken);
        Assert.Equal(1, store.ClearCallCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ReturnsNull_WhenSessionStoreLoadThrows()
    {
        var store = new TestSessionStore
        {
            LoadException = new NullReferenceException("Simulated store failure.")
        };
        var service = new DesktopAuthService(store, new DesktopAuthOptions(), new TestOidcClient());

        var accessToken = await service.GetAccessTokenAsync();

        Assert.Null(accessToken);
        Assert.Equal(1, store.ClearCallCount);
    }

    [Fact]
    public async Task TryRestoreSessionAsync_ReturnsNull_AndClears_WhenStoredSessionHasDefaultExpiry()
    {
        var store = new TestSessionStore
        {
            LoadResult = new AuthSession
            {
                AccessToken = "valid-token",
                ExpiresAtUtc = default
            }
        };

        var service = new DesktopAuthService(store, new DesktopAuthOptions(), new TestOidcClient());

        var restored = await service.TryRestoreSessionAsync();

        Assert.Null(restored);
        Assert.Equal(1, store.ClearCallCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ReturnsNull_AndClears_WhenStoredSessionHasDefaultExpiry()
    {
        var store = new TestSessionStore
        {
            LoadResult = new AuthSession
            {
                AccessToken = "valid-token",
                RefreshToken = "refresh-token",
                ExpiresAtUtc = default
            }
        };

        var service = new DesktopAuthService(store, new DesktopAuthOptions(), new TestOidcClient());

        var accessToken = await service.GetAccessTokenAsync(forceRefresh: true);

        Assert.Null(accessToken);
        Assert.Equal(1, store.ClearCallCount);
    }

    [Fact]
    public async Task SignInAsync_ReturnsFailure_WhenOidcResponseIsNull()
    {
        var store = new TestSessionStore();
        var oidc = new TestOidcClient
        {
            ReturnNullLoginResult = true
        };
        var service = new DesktopAuthService(store, new DesktopAuthOptions(), oidc);

        var result = await service.SignInAsync();

        Assert.False(result.Succeeded);
        Assert.False(result.Cancelled);
        Assert.Equal("Sign-in failed: empty provider response.", result.Message);
        Assert.Equal(0, store.SaveCallCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ConcurrentExpiredSessionRequests_AttemptSingleRefresh()
    {
        var store = new TestSessionStore
        {
            LoadResult = new AuthSession
            {
                AccessToken = "expired-token",
                RefreshToken = "refresh-token",
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10),
                Subject = "user-a"
            }
        };
        var oidc = new TestOidcClient
        {
            RefreshException = new InvalidOperationException("Simulated refresh failure."),
            RefreshDelayMilliseconds = 150
        };
        var service = new DesktopAuthService(store, new DesktopAuthOptions(), oidc);

        var firstRequest = service.GetAccessTokenAsync(forceRefresh: true);
        var secondRequest = service.GetAccessTokenAsync(forceRefresh: true);
        await Task.WhenAll(firstRequest, secondRequest);

        Assert.Null(firstRequest.Result);
        Assert.Null(secondRequest.Result);
        Assert.Equal(1, oidc.RefreshCallCount);
        Assert.True(store.ClearCallCount >= 1);
    }

    private sealed class TestSessionStore : IAuthSessionStore
    {
        public AuthSession? LoadResult { get; set; }

        public Exception? LoadException { get; init; }

        public int ClearCallCount { get; private set; }

        public int SaveCallCount { get; private set; }

        public AuthSession? SavedSession { get; private set; }

        public Task<AuthSession?> LoadAsync(CancellationToken cancellationToken = default)
        {
            if (LoadException is not null)
            {
                throw LoadException;
            }

            return Task.FromResult(LoadResult);
        }

        public Task SaveAsync(AuthSession session, CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            SavedSession = session;
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            ClearCallCount++;
            LoadResult = null;
            return Task.CompletedTask;
        }
    }

    private sealed class TestOidcClient : IDesktopOidcClient
    {
        public LoginResult? LoginResult { get; init; }

        public Exception? LoginException { get; init; }

        public bool ReturnNullLoginResult { get; init; }

        public Exception? RefreshException { get; init; }

        public RefreshTokenResult? RefreshResult { get; init; }

        public int RefreshDelayMilliseconds { get; init; }

        public int RefreshCallCount { get; private set; }

        public Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            if (LoginException is not null)
            {
                throw LoginException;
            }

            if (ReturnNullLoginResult)
            {
                return Task.FromResult<LoginResult>(null!);
            }

            return Task.FromResult(LoginResult ?? new LoginResult { Error = "Test login response not configured." });
        }

        public async Task<RefreshTokenResult> RefreshTokenAsync(string refreshToken)
        {
            RefreshCallCount++;
            if (RefreshDelayMilliseconds > 0)
            {
                await Task.Delay(RefreshDelayMilliseconds);
            }

            if (RefreshException is not null)
            {
                throw RefreshException;
            }

            return RefreshResult!;
        }
    }
}
