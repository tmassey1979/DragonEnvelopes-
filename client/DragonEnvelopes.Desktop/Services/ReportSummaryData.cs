namespace DragonEnvelopes.Desktop.Services;

public sealed record ReportSummaryData(
    decimal NetWorth,
    decimal MonthlySpend,
    decimal RemainingBudget,
    decimal EnvelopeCoveragePercent);
