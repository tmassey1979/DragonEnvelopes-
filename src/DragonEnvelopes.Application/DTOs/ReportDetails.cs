namespace DragonEnvelopes.Application.DTOs;

public sealed record EnvelopeBalanceReportDetails(
    Guid EnvelopeId,
    string EnvelopeName,
    decimal MonthlyBudget,
    decimal CurrentBalance,
    bool IsArchived);

public sealed record MonthlySpendReportPointDetails(
    string Month,
    decimal TotalSpend);

public sealed record CategoryBreakdownReportItemDetails(
    string Category,
    decimal TotalSpend);
