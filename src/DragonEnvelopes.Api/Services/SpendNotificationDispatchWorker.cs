using DragonEnvelopes.Application.Services;

namespace DragonEnvelopes.Api.Services;

public sealed class SpendNotificationDispatchWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<SpendNotificationDispatchWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var dispatchService = scope.ServiceProvider.GetRequiredService<ISpendNotificationDispatchService>();
                var result = await dispatchService.DispatchPendingAsync(stoppingToken);
                if (result.SentCount > 0 || result.FailedCount > 0)
                {
                    logger.LogInformation(
                        "Spend notification dispatch cycle completed. Sent={SentCount}, Failed={FailedCount}",
                        result.SentCount,
                        result.FailedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Spend notification dispatch worker loop failed.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
