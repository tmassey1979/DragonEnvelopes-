namespace DragonEnvelopes.Application.Services;

public interface IStripeWebhookService
{
    Task<StripeWebhookProcessResult> ProcessAsync(
        string payload,
        string? stripeSignatureHeader,
        CancellationToken cancellationToken = default);
}
