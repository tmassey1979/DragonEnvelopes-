namespace DragonEnvelopes.Ledger.Api.Services;

public sealed class LedgerOutboxDispatchWorkerOptions
{
    public bool Enabled { get; set; } = true;

    public int PollIntervalSeconds { get; set; } = 5;

    public int BatchSize { get; set; } = 50;

    public int BacklogWarningThreshold { get; set; } = 100;
}
