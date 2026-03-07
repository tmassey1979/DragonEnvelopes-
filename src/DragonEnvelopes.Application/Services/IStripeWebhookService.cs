namespace DragonEnvelopes.Application.Services;

public interface IStripeWebhookService
{
    Task<StripeWebhookProcessResult> ProcessAsync(
        string payload,
        string? stripeSignatureHeader,
        CancellationToken cancellationToken = default);

    Task<StripeWebhookReplayResult> ReplayFailedEventAsync(
        Guid familyId,
        Guid webhookEventId,
        CancellationToken cancellationToken = default);
}
