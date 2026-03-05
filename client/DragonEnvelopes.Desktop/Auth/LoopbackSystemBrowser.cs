using System.Diagnostics;
using System.Net;
using System.Text;
using IdentityModel.OidcClient.Browser;

namespace DragonEnvelopes.Desktop.Auth;

public sealed class LoopbackSystemBrowser : IBrowser
{
    private readonly string _redirectUri;
    private readonly string _listenerPrefix;

    public LoopbackSystemBrowser(string redirectUri)
    {
        _redirectUri = redirectUri;
        _listenerPrefix = EnsureListenerPrefix(redirectUri);
    }

    public async Task<BrowserResult> InvokeAsync(
        BrowserOptions options,
        CancellationToken cancellationToken = default)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add(_listenerPrefix);
        listener.Start();

        OpenBrowser(options.StartUrl);

        try
        {
            using var timeoutCts = new CancellationTokenSource(options.Timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var contextTask = listener.GetContextAsync();
            var completedTask = await Task.WhenAny(contextTask, Task.Delay(Timeout.InfiniteTimeSpan, linkedCts.Token));

            if (completedTask != contextTask)
            {
                return new BrowserResult
                {
                    ResultType = BrowserResultType.Timeout,
                    Error = "Timeout waiting for browser callback."
                };
            }

            var context = await contextTask;
            await WriteBrowserCompletionResponse(context.Response);

            return new BrowserResult
            {
                ResultType = BrowserResultType.Success,
                Response = context.Request.Url?.AbsoluteUri ?? _redirectUri
            };
        }
        catch (OperationCanceledException)
        {
            return new BrowserResult
            {
                ResultType = BrowserResultType.UserCancel,
                Error = "Browser sign-in canceled."
            };
        }
        catch (Exception ex)
        {
            return new BrowserResult
            {
                ResultType = BrowserResultType.UnknownError,
                Error = ex.Message
            };
        }
    }

    private static void OpenBrowser(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    private static async Task WriteBrowserCompletionResponse(HttpListenerResponse response)
    {
        const string html = """
                            <html><body style="font-family:Segoe UI;background:#0b1220;color:#f4f7ff">
                            <h2>DragonEnvelopes sign-in complete</h2>
                            <p>You can close this window and return to the desktop app.</p>
                            </body></html>
                            """;

        var bytes = Encoding.UTF8.GetBytes(html);
        response.ContentType = "text/html";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
        response.OutputStream.Close();
    }

    private static string EnsureListenerPrefix(string redirectUri)
    {
        var uri = new Uri(redirectUri, UriKind.Absolute);
        var builder = new UriBuilder(uri)
        {
            Path = uri.AbsolutePath.EndsWith('/') ? uri.AbsolutePath : $"{uri.AbsolutePath}/"
        };

        return builder.Uri.AbsoluteUri;
    }
}
