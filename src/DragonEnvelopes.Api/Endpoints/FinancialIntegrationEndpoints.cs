namespace DragonEnvelopes.Api.Endpoints;

internal static partial class FinancialIntegrationEndpoints
{
    public static RouteGroupBuilder MapFinancialIntegrationEndpoints(this RouteGroupBuilder v1)
    {
        v1.AddEndpointFilterFactory(static (_, next) => async invocationContext =>
        {
            invocationContext.HttpContext.Response.Headers["X-Trace-Id"] = invocationContext.HttpContext.TraceIdentifier;
            return await next(invocationContext);
        });

        MapWebhookAndNotificationEndpoints(v1);
        MapProviderActivityEndpoints(v1);
        MapPlaidEndpoints(v1);
        MapStripeAccountEndpoints(v1);
        MapEnvelopeCardEndpoints(v1);

        return v1;
    }

    private static string? TrimActivityError(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= 240
            ? normalized
            : $"{normalized[..240]}...";
    }
}
