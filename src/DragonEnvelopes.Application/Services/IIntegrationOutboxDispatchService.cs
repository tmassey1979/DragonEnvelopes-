namespace DragonEnvelopes.Application.Services;

public interface IIntegrationOutboxDispatchService
{
    Task<IntegrationOutboxDispatchResult> DispatchPendingAsync(
        int take,
        CancellationToken cancellationToken = default);
}

public sealed record IntegrationOutboxDispatchResult(
    int LoadedCount,
    int PublishedCount,
    int FailedCount,
    int PendingCount);
