namespace DragonEnvelopes.Desktop.Auth;

public sealed class AuthSession
{
    public string AccessToken { get; init; } = string.Empty;

    public string? RefreshToken { get; init; }

    public string? IdentityToken { get; init; }

    public DateTimeOffset ExpiresAtUtc { get; init; }

    public string? Subject { get; init; }

    public bool IsExpired(DateTimeOffset nowUtc) => nowUtc >= ExpiresAtUtc;
}
