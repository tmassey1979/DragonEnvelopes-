using DragonEnvelopes.Application.Services;

namespace DragonEnvelopes.Financial.Api.Services;

public sealed class PlaidBalanceRefreshWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<PlaidBalanceRefreshWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IPlaidBalanceReconciliationService>();
                var results = await service.RefreshConnectedFamiliesAsync(stoppingToken);
                if (results.Count > 0)
                {
                    logger.LogInformation("Plaid balance refresh worker completed cycle for {FamilyCount} families.", results.Count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Plaid balance refresh worker loop failed.");
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

