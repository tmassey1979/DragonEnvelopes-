namespace DragonEnvelopes.Desktop.Services;

public interface IDesktopPlaidLinkService
{
    Task<DesktopPlaidLinkResult> LaunchAsync(
        string linkToken,
        CancellationToken cancellationToken = default);
}
