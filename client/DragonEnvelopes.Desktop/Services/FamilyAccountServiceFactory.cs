using System.Net.Http;
using DragonEnvelopes.Desktop.Api;
using DragonEnvelopes.Desktop.Auth;

namespace DragonEnvelopes.Desktop.Services;

public static class FamilyAccountServiceFactory
{
    public static IFamilyAccountService CreateDefault()
    {
        var apiOptions = new ApiClientOptions();
        var authService = new DesktopAuthService(new ProtectedTokenSessionStore());
        var handler = new AuthenticatedApiHttpMessageHandler(authService)
        {
            InnerHandler = new HttpClientHandler()
        };

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(apiOptions.BaseUrl, UriKind.Absolute)
        };

        return new FamilyAccountService(new DragonEnvelopesApiClient(httpClient));
    }
}
