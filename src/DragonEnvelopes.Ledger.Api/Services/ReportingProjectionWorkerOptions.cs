namespace DragonEnvelopes.Ledger.Api.Services;

public sealed class ReportingProjectionWorkerOptions
{
    public bool Enabled { get; set; } = true;

    public int PollIntervalSeconds { get; set; } = 5;

    public int BatchSize { get; set; } = 100;

    public int BacklogWarningThreshold { get; set; } = 100;
}
