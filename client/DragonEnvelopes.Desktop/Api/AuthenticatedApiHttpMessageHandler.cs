using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using DragonEnvelopes.Desktop.Auth;

namespace DragonEnvelopes.Desktop.Api;

public sealed class AuthenticatedApiHttpMessageHandler : DelegatingHandler
{
    private readonly IAuthService _authService;

    public AuthenticatedApiHttpMessageHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await AttachTokenAsync(request, forceRefresh: false, cancellationToken);

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized || !IsSafeToRetry(request))
        {
            return response;
        }

        response.Dispose();

        var retryRequest = await CloneForSafeRetryAsync(request, cancellationToken);
        await AttachTokenAsync(retryRequest, forceRefresh: true, cancellationToken);
        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private async Task AttachTokenAsync(
        HttpRequestMessage request,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var token = await _authService.GetAccessTokenAsync(forceRefresh, cancellationToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private static bool IsSafeToRetry(HttpRequestMessage request)
    {
        return request.Method == HttpMethod.Get
            || request.Method == HttpMethod.Head
            || request.Method == HttpMethod.Options;
    }

    private static async Task<HttpRequestMessage> CloneForSafeRetryAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content is not null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            clone.Content = new ByteArrayContent(contentBytes);

            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
}
