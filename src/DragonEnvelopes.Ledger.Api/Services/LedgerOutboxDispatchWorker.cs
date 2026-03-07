using DragonEnvelopes.Application.Services;
using Microsoft.Extensions.Options;

namespace DragonEnvelopes.Ledger.Api.Services;

public sealed class LedgerOutboxDispatchWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<LedgerOutboxDispatchWorkerOptions> optionsAccessor,
    ILogger<LedgerOutboxDispatchWorker> logger) : BackgroundService
{
    private readonly LedgerOutboxDispatchWorkerOptions _options = optionsAccessor.Value;
    private readonly string[] _sourceServices = ResolveSourceServices(optionsAccessor.Value.SourceServices);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Ledger outbox dispatch worker is disabled by configuration.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dispatchService = scope.ServiceProvider.GetRequiredService<IIntegrationOutboxDispatchService>();
                foreach (var sourceService in _sourceServices)
                {
                    var result = await dispatchService.DispatchPendingAsync(
                        sourceService,
                        Math.Clamp(_options.BatchSize, 1, 500),
                        stoppingToken);

                    if (result.LoadedCount > 0 || result.PendingCount > 0)
                    {
                        logger.LogInformation(
                            "Ledger outbox dispatch cycle completed. Source={Source}, Loaded={Loaded}, Published={Published}, Failed={Failed}, Pending={Pending}",
                            sourceService,
                            result.LoadedCount,
                            result.PublishedCount,
                            result.FailedCount,
                            result.PendingCount);
                    }

                    if (result.PendingCount >= Math.Max(1, _options.BacklogWarningThreshold))
                    {
                        logger.LogWarning(
                            "Ledger outbox backlog threshold exceeded. Source={Source}, Pending={Pending}, Threshold={Threshold}",
                            sourceService,
                            result.PendingCount,
                            _options.BacklogWarningThreshold);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ledger outbox dispatch worker loop failed.");
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

    private static string[] ResolveSourceServices(IEnumerable<string>? configuredSourceServices)
    {
        var normalized = (configuredSourceServices ?? [])
            .Where(static source => !string.IsNullOrWhiteSpace(source))
            .Select(static source => source.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return normalized.Length == 0
            ? ["ledger-api"]
            : normalized;
    }
}
