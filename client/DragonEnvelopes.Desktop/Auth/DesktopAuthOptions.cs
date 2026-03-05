namespace DragonEnvelopes.Desktop.Auth;

public sealed class DesktopAuthOptions
{
    public string Authority { get; init; } =
        Environment.GetEnvironmentVariable("DRAGONENVELOPES_AUTH_AUTHORITY")
        ?? "http://localhost:18080/realms/dragonenvelopes";

    public string ClientId { get; init; } =
        Environment.GetEnvironmentVariable("DRAGONENVELOPES_AUTH_CLIENT_ID")
        ?? "dragonenvelopes-desktop";

    public string Scope { get; init; } =
        Environment.GetEnvironmentVariable("DRAGONENVELOPES_AUTH_SCOPE")
        ?? "openid profile email offline_access";

    public string RedirectUri { get; init; } =
        Environment.GetEnvironmentVariable("DRAGONENVELOPES_AUTH_REDIRECT_URI")
        ?? "http://127.0.0.1:7890/callback/";

    public TimeSpan SignInTimeout { get; init; } = TimeSpan.FromMinutes(2);
}
