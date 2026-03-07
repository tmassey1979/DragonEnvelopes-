namespace DragonEnvelopes.Api.Services;

public sealed class RecurringBillAutoPostWorker(
    IRecurringAutoPostService recurringAutoPostService,
    ILogger<RecurringBillAutoPostWorker> logger) : BackgroundService
{
    private static readonly TimeSpan LoopInterval = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(LoopInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessDueRecurringBillsAsync(stoppingToken);
            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task ProcessDueRecurringBillsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var summary = await recurringAutoPostService.RunAsync(
                familyId: null,
                dueDate: null,
                cancellationToken);

            logger.LogInformation(
                "Recurring auto-post cycle complete for {DueDate}. Due={DueBillCount}, Posted={PostedCount}, Skipped={SkippedCount}, Failed={FailedCount}, AlreadyProcessed={AlreadyProcessedCount}.",
                summary.DueDate,
                summary.DueBillCount,
                summary.PostedCount,
                summary.SkippedCount,
                summary.FailedCount,
                summary.AlreadyProcessedCount);
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation and allow graceful shutdown.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Recurring auto-post worker loop failed.");
        }
    }
}
