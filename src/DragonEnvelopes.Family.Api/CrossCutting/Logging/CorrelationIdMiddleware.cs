using Microsoft.Extensions.Primitives;
using Serilog.Context;

namespace DragonEnvelopes.Family.Api.CrossCutting.Logging;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context.Request.Headers[HeaderName]);

        context.TraceIdentifier = correlationId;
        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    private static string ResolveCorrelationId(StringValues headerValues)
    {
        var provided = headerValues.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(provided))
        {
            var normalized = provided.Trim();
            if (normalized.Length <= 128)
            {
                return normalized;
            }
        }

        return Guid.NewGuid().ToString("N");
    }
}
