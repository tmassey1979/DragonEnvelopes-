using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.Tests;

internal sealed class FakeDesktopPlaidLinkService : IDesktopPlaidLinkService
{
    public DesktopPlaidLinkResult NextResult { get; set; } = DesktopPlaidLinkResult.Canceled("Canceled in fake service.");

    public string LastLinkToken { get; private set; } = string.Empty;

    public int LaunchCallCount { get; private set; }

    public Task<DesktopPlaidLinkResult> LaunchAsync(string linkToken, CancellationToken cancellationToken = default)
    {
        LaunchCallCount += 1;
        LastLinkToken = linkToken;
        return Task.FromResult(NextResult);
    }
}
