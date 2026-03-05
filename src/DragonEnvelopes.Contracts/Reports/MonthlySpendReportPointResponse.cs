namespace DragonEnvelopes.Contracts.Reports;

public sealed record MonthlySpendReportPointResponse(
    string Month,
    decimal TotalSpend);
