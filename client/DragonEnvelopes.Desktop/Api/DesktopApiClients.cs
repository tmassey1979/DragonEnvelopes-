namespace DragonEnvelopes.Desktop.Api;

public sealed class DesktopApiClients
{
    public DesktopApiClients(IBackendApiClient family, IBackendApiClient ledger, IBackendApiClient financial)
    {
        Family = family;
        Ledger = ledger;
        Financial = financial;
    }

    public IBackendApiClient Family { get; }

    public IBackendApiClient Ledger { get; }

    public IBackendApiClient Financial { get; }
}
