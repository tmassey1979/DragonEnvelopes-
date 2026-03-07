using DragonEnvelopes.Application.Services;

namespace DragonEnvelopes.Financial.Api.Services;

public sealed class PlaidTransactionSyncWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<PlaidTransactionSyncWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var syncService = scope.ServiceProvider.GetRequiredService<IPlaidTransactionSyncService>();
                var results = await syncService.SyncConnectedFamiliesAsync(stoppingToken);
                if (results.Count > 0)
                {
                    logger.LogInformation(
                        "Plaid sync worker completed cycle for {FamilyCount} families.",
                        results.Count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Plaid transaction sync worker loop failed.");
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

