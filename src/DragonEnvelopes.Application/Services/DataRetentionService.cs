using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using Microsoft.Extensions.Options;

namespace DragonEnvelopes.Application.Services;

public sealed class DataRetentionService(
    IStripeWebhookEventRepository stripeWebhookEventRepository,
    ISpendNotificationEventRepository spendNotificationEventRepository,
    IClock clock,
    IOptions<DataRetentionOptions> optionsAccessor) : IDataRetentionService
{
    public async Task<DataRetentionCleanupResult> CleanupAsync(CancellationToken cancellationToken = default)
    {
        var options = optionsAccessor.Value;

        var batchSize = Math.Max(1, options.BatchSize);
        var webhookRetentionDays = Math.Max(1, options.StripeWebhookRetentionDays);
        var notificationRetentionDays = Math.Max(1, options.SpendNotificationRetentionDays);

        var now = clock.UtcNow;
        var webhookCutoff = now.AddDays(-webhookRetentionDays);
        var notificationCutoff = now.AddDays(-notificationRetentionDays);

        var deletedWebhookEvents = await stripeWebhookEventRepository.DeleteProcessedBeforeAsync(
            webhookCutoff,
            batchSize,
            cancellationToken);

        var deletedNotificationEvents = await spendNotificationEventRepository.DeleteTerminalBeforeAsync(
            notificationCutoff,
            batchSize,
            cancellationToken);

        return new DataRetentionCleanupResult(
            now,
            webhookCutoff,
            notificationCutoff,
            deletedWebhookEvents,
            deletedNotificationEvents);
    }
}
