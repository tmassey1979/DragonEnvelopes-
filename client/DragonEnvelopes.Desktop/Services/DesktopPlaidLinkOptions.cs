namespace DragonEnvelopes.Desktop.Services;

public sealed class DesktopPlaidLinkOptions
{
    public string ListenerUri { get; init; } =
        Environment.GetEnvironmentVariable("DRAGONENVELOPES_PLAID_LINK_REDIRECT_URI")
        ?? "http://127.0.0.1:7891/";

    public TimeSpan BrowserTimeout { get; init; } = TimeSpan.FromMinutes(5);
}
