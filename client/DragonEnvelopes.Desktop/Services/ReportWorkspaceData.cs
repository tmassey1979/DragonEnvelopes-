namespace DragonEnvelopes.Desktop.Services;

public sealed record ReportWorkspaceData(
    ReportSummaryData? Summary,
    IReadOnlyList<ReportEnvelopeBalanceData> EnvelopeBalances,
    IReadOnlyList<ReportMonthlySpendData> MonthlySpend,
    IReadOnlyList<ReportCategoryBreakdownData> CategoryBreakdown);

public sealed record ReportEnvelopeBalanceData(
    string EnvelopeName,
    decimal MonthlyBudget,
    decimal CurrentBalance,
    bool IsArchived);

public sealed record ReportMonthlySpendData(
    string Month,
    decimal TotalSpend);

public sealed record ReportCategoryBreakdownData(
    string Category,
    decimal TotalSpend);
