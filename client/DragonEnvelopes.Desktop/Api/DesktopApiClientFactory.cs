using System.Net.Http;
using DragonEnvelopes.Desktop.Auth;

namespace DragonEnvelopes.Desktop.Api;

public static class DesktopApiClientFactory
{
    public static DesktopApiClients Create(IAuthService authService, ApiClientOptions options)
    {
        var familyClient = CreateClient(authService, options.ResolveFamilyBaseUrl());
        var ledgerClient = CreateClient(authService, options.ResolveLedgerBaseUrl());
        return new DesktopApiClients(familyClient, ledgerClient);
    }

    private static IBackendApiClient CreateClient(IAuthService authService, string baseUrl)
    {
        var handler = new AuthenticatedApiHttpMessageHandler(authService)
        {
            InnerHandler = new HttpClientHandler()
        };

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl, UriKind.Absolute)
        };

        return new DragonEnvelopesApiClient(httpClient);
    }
}
