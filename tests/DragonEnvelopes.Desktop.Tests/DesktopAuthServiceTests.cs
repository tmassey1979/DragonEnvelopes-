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

    private sealed class TestSessionStore : IAuthSessionStore
    {
        public AuthSession? LoadResult { get; init; }

        public Exception? LoadException { get; init; }

        public int ClearCallCount { get; private set; }

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
            SavedSession = session;
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            ClearCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class TestOidcClient : IDesktopOidcClient
    {
        public Exception? RefreshException { get; init; }

        public RefreshTokenResult? RefreshResult { get; init; }

        public Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Not needed for this test.");
        }

        public Task<RefreshTokenResult> RefreshTokenAsync(string refreshToken)
        {
            if (RefreshException is not null)
            {
                throw RefreshException;
            }

            return Task.FromResult(RefreshResult!);
        }
    }
}
