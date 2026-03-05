namespace DragonEnvelopes.Contracts.Reports;

public sealed record CategoryBreakdownReportItemResponse(
    string Category,
    decimal TotalSpend);
