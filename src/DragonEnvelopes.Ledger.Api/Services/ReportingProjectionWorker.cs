using DragonEnvelopes.Application.Services;
using Microsoft.Extensions.Options;

namespace DragonEnvelopes.Ledger.Api.Services;

public sealed class ReportingProjectionWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ReportingProjectionWorkerOptions> optionsAccessor,
    ILogger<ReportingProjectionWorker> logger) : BackgroundService
{
    private readonly ReportingProjectionWorkerOptions _options = optionsAccessor.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Reporting projection worker is disabled by configuration.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var projectionService = scope.ServiceProvider.GetRequiredService<IReportingProjectionService>();
                var result = await projectionService.ProjectPendingAsync(
                    Math.Clamp(_options.BatchSize, 1, 2000),
                    stoppingToken);

                if (result.LoadedCount > 0 || result.RemainingCount > 0)
                {
                    logger.LogInformation(
                        "Reporting projection batch completed. Loaded={Loaded}, Applied={Applied}, Failed={Failed}, Remaining={Remaining}",
                        result.LoadedCount,
                        result.AppliedCount,
                        result.FailedCount,
                        result.RemainingCount);
                }

                if (result.RemainingCount >= Math.Max(1, _options.BacklogWarningThreshold))
                {
                    logger.LogWarning(
                        "Reporting projection backlog threshold exceeded. Pending={Pending}, Threshold={Threshold}",
                        result.RemainingCount,
                        _options.BacklogWarningThreshold);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reporting projection worker loop failed.");
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
