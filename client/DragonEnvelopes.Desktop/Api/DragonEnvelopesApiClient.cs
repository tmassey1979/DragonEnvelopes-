using System.Net.Http;

namespace DragonEnvelopes.Desktop.Api;

public sealed class DragonEnvelopesApiClient : IBackendApiClient, IDisposable
{
    private readonly HttpClient _httpClient;

    public DragonEnvelopesApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        return _httpClient.SendAsync(request, cancellationToken);
    }

    public Task<HttpResponseMessage> GetAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        return _httpClient.GetAsync(relativePath, cancellationToken);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
