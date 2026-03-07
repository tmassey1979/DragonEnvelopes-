namespace DragonEnvelopes.Api.Services;

public sealed class RecurringAutoPostWorkerOptions
{
    public bool Enabled { get; init; } = true;

    public int PollIntervalMinutes { get; init; } = 30;

    public bool UseDistributedLock { get; init; } = true;

    public long DistributedLockKey { get; init; } = 2147482647;
}
