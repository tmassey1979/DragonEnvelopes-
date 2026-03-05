namespace DragonEnvelopes.Contracts.Reports;

public sealed record EnvelopeBalanceReportResponse(
    Guid EnvelopeId,
    string EnvelopeName,
    decimal MonthlyBudget,
    decimal CurrentBalance,
    bool IsArchived);
