using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;

namespace DragonEnvelopes.Desktop.Services;

public sealed class DesktopPlaidLinkService : IDesktopPlaidLinkService
{
    private readonly DesktopPlaidLinkOptions _options;

    public DesktopPlaidLinkService(DesktopPlaidLinkOptions? options = null)
    {
        _options = options ?? new DesktopPlaidLinkOptions();
    }

    public async Task<DesktopPlaidLinkResult> LaunchAsync(
        string linkToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(linkToken))
        {
            return DesktopPlaidLinkResult.Failure("Plaid link token is required.");
        }

        var listenerPrefix = EnsureListenerPrefix(_options.ListenerUri);
        var launchUri = new Uri(new Uri(listenerPrefix), "launch");
        var callbackUri = new Uri(new Uri(listenerPrefix), "callback");
        var state = Guid.NewGuid().ToString("N");

        using var listener = new HttpListener();
        listener.Prefixes.Add(listenerPrefix);

        try
        {
            listener.Start();
        }
        catch (HttpListenerException ex)
        {
            return DesktopPlaidLinkResult.Failure($"Unable to start local Plaid Link listener: {ex.Message}");
        }

        OpenBrowser(launchUri.AbsoluteUri);

        try
        {
            using var timeoutCts = new CancellationTokenSource(_options.BrowserTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            while (true)
            {
                var context = await listener.GetContextAsync().WaitAsync(linkedCts.Token);
                var route = context.Request.Url?.AbsolutePath.Trim('/').ToLowerInvariant() ?? string.Empty;

                if (route == "launch")
                {
                    await WriteLaunchResponseAsync(context.Response, linkToken, state, callbackUri.AbsoluteUri);
                    continue;
                }

                if (route != "callback")
                {
                    await WriteTextResponseAsync(context.Response, HttpStatusCode.NotFound, "Route not found.");
                    continue;
                }

                var query = ParseQuery(context.Request.Url?.Query);
                if (!query.TryGetValue("state", out var callbackState)
                    || !string.Equals(callbackState, state, StringComparison.Ordinal))
                {
                    await WriteCompletionResponseAsync(context.Response, false, "Session validation failed.");
                    return DesktopPlaidLinkResult.Failure("Plaid Link callback state was invalid.");
                }

                if (query.TryGetValue("result", out var result)
                    && string.Equals(result, "cancel", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteCompletionResponseAsync(context.Response, false, "Plaid Link canceled.");
                    return DesktopPlaidLinkResult.Canceled("Plaid Link was canceled.");
                }

                if (query.TryGetValue("error", out var errorMessage)
                    && !string.IsNullOrWhiteSpace(errorMessage))
                {
                    await WriteCompletionResponseAsync(context.Response, false, "Plaid Link failed.");
                    return DesktopPlaidLinkResult.Failure($"Plaid Link failed: {errorMessage}");
                }

                if (!query.TryGetValue("public_token", out var publicToken)
                    || string.IsNullOrWhiteSpace(publicToken))
                {
                    await WriteCompletionResponseAsync(context.Response, false, "No public token was returned.");
                    return DesktopPlaidLinkResult.Failure("Plaid Link did not return a public token.");
                }

                await WriteCompletionResponseAsync(context.Response, true, "Plaid Link complete. You can close this window.");
                return DesktopPlaidLinkResult.Success(publicToken);
            }
        }
        catch (OperationCanceledException)
        {
            return DesktopPlaidLinkResult.Canceled("Plaid Link canceled or timed out.");
        }
        catch (Exception ex)
        {
            return DesktopPlaidLinkResult.Failure($"Plaid Link failed: {ex.Message}");
        }
        finally
        {
            if (listener.IsListening)
            {
                listener.Stop();
            }
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

    private static async Task WriteLaunchResponseAsync(
        HttpListenerResponse response,
        string linkToken,
        string state,
        string callbackUrl)
    {
        var escapedToken = JavaScriptEncoder.Default.Encode(linkToken);
        var escapedState = JavaScriptEncoder.Default.Encode(state);
        var escapedCallback = JavaScriptEncoder.Default.Encode(callbackUrl);

        var html = $$"""
                     <!doctype html>
                     <html lang="en">
                     <head>
                       <meta charset="utf-8" />
                       <meta name="viewport" content="width=device-width, initial-scale=1" />
                       <title>DragonEnvelopes Plaid Link</title>
                       <style>
                         body { font-family: Segoe UI, sans-serif; background:#0f1115; color:#f5f7ff; margin:0; padding:24px; }
                         .panel { max-width: 680px; margin: 0 auto; border:1px solid #2a2e39; border-radius: 12px; padding:20px; background:#171b22; }
                         h2 { margin-top:0; }
                         p { color:#c8cde0; }
                         button { background:#ca1717; color:#fff; border:0; border-radius:8px; padding:10px 14px; cursor:pointer; }
                       </style>
                       <script src="https://cdn.plaid.com/link/v2/stable/link-initialize.js"></script>
                     </head>
                     <body>
                       <div class="panel">
                         <h2>Connect Your Bank</h2>
                         <p>DragonEnvelopes will open Plaid Link in this browser window.</p>
                         <button id="openLinkButton" type="button">Open Plaid Link</button>
                         <p id="statusText">Preparing secure session...</p>
                       </div>
                       <script>
                         const linkToken = "{{escapedToken}}";
                         const callbackState = "{{escapedState}}";
                         const callbackUrl = "{{escapedCallback}}";
                         const statusText = document.getElementById("statusText");
                         const openButton = document.getElementById("openLinkButton");

                         function redirectWith(params) {
                           const query = new URLSearchParams(params);
                           window.location.assign(callbackUrl + "?" + query.toString());
                         }

                         const handler = Plaid.create({
                           token: linkToken,
                           onSuccess: (public_token) => {
                             statusText.textContent = "Plaid Link completed. Returning to app...";
                             redirectWith({ state: callbackState, public_token });
                           },
                           onExit: (error) => {
                             if (error && error.error_message) {
                               redirectWith({ state: callbackState, error: error.error_message });
                               return;
                             }

                             redirectWith({ state: callbackState, result: "cancel" });
                           }
                         });

                         openButton.addEventListener("click", () => handler.open());
                         handler.open();
                       </script>
                     </body>
                     </html>
                     """;

        await WriteHtmlResponseAsync(response, html);
    }

    private static Task WriteCompletionResponseAsync(
        HttpListenerResponse response,
        bool success,
        string detail)
    {
        var title = success ? "Plaid Link Complete" : "Plaid Link Ended";
        var escapedDetail = WebUtility.HtmlEncode(detail);
        var html = $$"""
                     <!doctype html>
                     <html lang="en">
                     <head>
                       <meta charset="utf-8" />
                       <title>{{title}}</title>
                       <style>
                         body { font-family: Segoe UI, sans-serif; background:#0f1115; color:#f5f7ff; margin:0; padding:24px; }
                         .panel { max-width: 580px; margin: 0 auto; border:1px solid #2a2e39; border-radius: 12px; padding:20px; background:#171b22; }
                       </style>
                     </head>
                     <body>
                       <div class="panel">
                         <h2>{{title}}</h2>
                         <p>{{escapedDetail}}</p>
                         <p>You can close this browser tab and return to DragonEnvelopes.</p>
                       </div>
                     </body>
                     </html>
                     """;

        return WriteHtmlResponseAsync(response, html);
    }

    private static async Task WriteHtmlResponseAsync(HttpListenerResponse response, string html)
    {
        var bytes = Encoding.UTF8.GetBytes(html);
        response.StatusCode = (int)HttpStatusCode.OK;
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
        response.OutputStream.Close();
    }

    private static async Task WriteTextResponseAsync(
        HttpListenerResponse response,
        HttpStatusCode statusCode,
        string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        response.StatusCode = (int)statusCode;
        response.ContentType = "text/plain; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
        response.OutputStream.Close();
    }

    private static string EnsureListenerPrefix(string listenerUri)
    {
        var uri = new Uri(listenerUri, UriKind.Absolute);
        var builder = new UriBuilder(uri)
        {
            Path = uri.AbsolutePath.EndsWith('/') ? uri.AbsolutePath : $"{uri.AbsolutePath}/"
        };

        return builder.Uri.AbsoluteUri;
    }

    private static IReadOnlyDictionary<string, string> ParseQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var trimmed = query.StartsWith('?') ? query[1..] : query;
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = pair.IndexOf('=');
            if (separatorIndex < 0)
            {
                var keyOnly = WebUtility.UrlDecode(pair);
                if (!string.IsNullOrWhiteSpace(keyOnly))
                {
                    values[keyOnly] = string.Empty;
                }

                continue;
            }

            var key = WebUtility.UrlDecode(pair[..separatorIndex]);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var value = WebUtility.UrlDecode(pair[(separatorIndex + 1)..]) ?? string.Empty;
            values[key] = value;
        }

        return values;
    }
}
