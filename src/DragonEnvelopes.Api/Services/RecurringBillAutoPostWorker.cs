using Microsoft.Extensions.Options;

namespace DragonEnvelopes.Api.Services;

public sealed class RecurringBillAutoPostWorker(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<RecurringAutoPostWorkerOptions> optionsAccessor,
    ILogger<RecurringBillAutoPostWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = optionsAccessor.Value;
        if (!options.Enabled)
        {
            logger.LogInformation("Recurring auto-post worker is disabled.");
            return;
        }

        var loopInterval = TimeSpan.FromMinutes(Math.Max(1, options.PollIntervalMinutes));
        logger.LogInformation(
            "Recurring auto-post worker started with PollIntervalMinutes={PollIntervalMinutes}, UseDistributedLock={UseDistributedLock}.",
            Math.Max(1, options.PollIntervalMinutes),
            options.UseDistributedLock);

        using var timer = new PeriodicTimer(loopInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessDueRecurringBillsAsync(cancellationToken: stoppingToken);
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

    private async Task ProcessDueRecurringBillsAsync(CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;

        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var workerLock = scope.ServiceProvider.GetRequiredService<IRecurringAutoPostWorkerLock>();
            await using var lease = await workerLock.TryAcquireAsync(cancellationToken);
            if (lease is null)
            {
                logger.LogDebug("Recurring auto-post cycle skipped because distributed lock was not acquired.");
                return;
            }

            var recurringAutoPostService = scope.ServiceProvider.GetRequiredService<IRecurringAutoPostService>();

            var summary = await recurringAutoPostService.RunAsync(
                familyId: null,
                dueDate: null,
                cancellationToken);

            var durationMs = Math.Round((DateTimeOffset.UtcNow - startedAtUtc).TotalMilliseconds, MidpointRounding.AwayFromZero);
            logger.LogInformation(
                "Recurring auto-post cycle complete for {DueDate}. Due={DueBillCount}, Posted={PostedCount}, Skipped={SkippedCount}, Failed={FailedCount}, AlreadyProcessed={AlreadyProcessedCount}, DurationMs={DurationMs}.",
                summary.DueDate,
                summary.DueBillCount,
                summary.PostedCount,
                summary.SkippedCount,
                summary.FailedCount,
                summary.AlreadyProcessedCount,
                durationMs);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Ignore cancellation and allow graceful shutdown.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Recurring auto-post worker loop failed.");
        }
    }
}
