using DragonEnvelopes.Application.Services;
using Microsoft.Extensions.Options;

namespace DragonEnvelopes.Financial.Api.Services;

public sealed class FinancialOutboxDispatchWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<FinancialOutboxDispatchWorkerOptions> optionsAccessor,
    ILogger<FinancialOutboxDispatchWorker> logger) : BackgroundService
{
    private const string FinancialSourceService = "financial-api";
    private readonly FinancialOutboxDispatchWorkerOptions _options = optionsAccessor.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Financial outbox dispatch worker is disabled by configuration.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dispatchService = scope.ServiceProvider.GetRequiredService<IIntegrationOutboxDispatchService>();
                var result = await dispatchService.DispatchPendingAsync(
                    FinancialSourceService,
                    Math.Clamp(_options.BatchSize, 1, 500),
                    stoppingToken);

                if (result.LoadedCount > 0 || result.PendingCount > 0)
                {
                    logger.LogInformation(
                        "Financial outbox dispatch cycle completed. Loaded={Loaded}, Published={Published}, Failed={Failed}, Pending={Pending}",
                        result.LoadedCount,
                        result.PublishedCount,
                        result.FailedCount,
                        result.PendingCount);
                }

                if (result.PendingCount >= Math.Max(1, _options.BacklogWarningThreshold))
                {
                    logger.LogWarning(
                        "Financial outbox backlog threshold exceeded. Pending={Pending}, Threshold={Threshold}",
                        result.PendingCount,
                        _options.BacklogWarningThreshold);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Financial outbox dispatch worker loop failed.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds)),
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
