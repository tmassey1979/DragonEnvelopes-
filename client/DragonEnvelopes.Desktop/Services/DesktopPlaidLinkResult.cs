namespace DragonEnvelopes.Desktop.Services;

public sealed record DesktopPlaidLinkResult(
    bool Succeeded,
    bool IsCanceled,
    string? PublicToken,
    string Message)
{
    public static DesktopPlaidLinkResult Success(string publicToken) =>
        new(
            Succeeded: true,
            IsCanceled: false,
            PublicToken: publicToken,
            Message: "Plaid Link completed.");

    public static DesktopPlaidLinkResult Canceled(string message) =>
        new(
            Succeeded: false,
            IsCanceled: true,
            PublicToken: null,
            Message: message);

    public static DesktopPlaidLinkResult Failure(string message) =>
        new(
            Succeeded: false,
            IsCanceled: false,
            PublicToken: null,
            Message: message);
}
