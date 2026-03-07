using DragonEnvelopes.Application.Services;
using Microsoft.Extensions.Options;

namespace DragonEnvelopes.Financial.Api.Services;

public sealed class DataRetentionWorker(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<DataRetentionOptions> optionsAccessor,
    ILogger<DataRetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = optionsAccessor.Value;
        if (!options.Enabled)
        {
            logger.LogInformation("Data retention worker is disabled.");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(5, options.PollIntervalMinutes));
        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCleanupCycleAsync(stoppingToken);

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunCleanupCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var retentionService = scope.ServiceProvider.GetRequiredService<IDataRetentionService>();
            var result = await retentionService.CleanupAsync(cancellationToken);

            if (result.DeletedStripeWebhookEvents > 0 || result.DeletedSpendNotificationEvents > 0)
            {
                logger.LogInformation(
                    "Data retention cleanup removed records. StripeWebhookEvents={StripeWebhookEvents}, SpendNotificationEvents={SpendNotificationEvents}, StripeCutoff={StripeCutoff}, NotificationCutoff={NotificationCutoff}",
                    result.DeletedStripeWebhookEvents,
                    result.DeletedSpendNotificationEvents,
                    result.StripeWebhookCutoffUtc,
                    result.SpendNotificationCutoffUtc);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // no-op during graceful shutdown
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Data retention cleanup cycle failed.");
        }
    }
}

