namespace DragonEnvelopes.Desktop.Api;

public sealed class DesktopApiClients
{
    public DesktopApiClients(IBackendApiClient family, IBackendApiClient ledger)
    {
        Family = family;
        Ledger = ledger;
    }

    public IBackendApiClient Family { get; }

    public IBackendApiClient Ledger { get; }
}
