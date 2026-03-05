using System.Net.Http;
using DragonEnvelopes.Desktop.Api;

namespace DragonEnvelopes.Desktop.Services;

public static class FamilyAccountServiceFactory
{
    public static IFamilyAccountService CreateDefault()
    {
        var apiOptions = new ApiClientOptions();
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiOptions.BaseUrl, UriKind.Absolute)
        };

        return new FamilyAccountService(new DragonEnvelopesApiClient(httpClient));
    }
}
