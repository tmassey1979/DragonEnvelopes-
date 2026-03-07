using DragonEnvelopes.Desktop.Api;
using DragonEnvelopes.Desktop.Auth;

namespace DragonEnvelopes.Desktop.Services;

public static class FamilyAccountServiceFactory
{
    public static IFamilyAccountService CreateDefault()
    {
        var apiOptions = new ApiClientOptions();
        var authService = new DesktopAuthService(new ProtectedTokenSessionStore());
        var apiClients = DesktopApiClientFactory.Create(authService, apiOptions);
        return new FamilyAccountService(apiClients.Family);
    }
}
