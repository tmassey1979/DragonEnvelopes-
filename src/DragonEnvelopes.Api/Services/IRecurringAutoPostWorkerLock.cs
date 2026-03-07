namespace DragonEnvelopes.Api.Services;

public interface IRecurringAutoPostWorkerLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(CancellationToken cancellationToken = default);
}
