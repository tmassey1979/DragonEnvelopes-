using System.Net.Http;

namespace DragonEnvelopes.Desktop.Api;

public interface IBackendApiClient
{
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);

    Task<HttpResponseMessage> GetAsync(string relativePath, CancellationToken cancellationToken = default);
}
