namespace DragonEnvelopes.Desktop.Services;

public interface IBudgetsDataService
{
    Task<BudgetAllocationWorkspaceData> GetWorkspaceAsync(
        string month,
        bool includeArchived,
        CancellationToken cancellationToken = default);

    Task<BudgetMonthSummaryData?> GetBudgetAsync(string month, CancellationToken cancellationToken = default);

    Task<BudgetMonthSummaryData> CreateBudgetAsync(string month, decimal totalIncome, CancellationToken cancellationToken = default);

    Task<BudgetMonthSummaryData> UpdateBudgetAsync(Guid budgetId, decimal totalIncome, CancellationToken cancellationToken = default);

    Task UpdateEnvelopeRolloverPolicyAsync(
        Guid envelopeId,
        string rolloverMode,
        decimal? rolloverCap,
        CancellationToken cancellationToken = default);

    Task<EnvelopeRolloverPreviewData> PreviewEnvelopeRolloverAsync(
        string month,
        CancellationToken cancellationToken = default);

    Task<EnvelopeRolloverApplyData> ApplyEnvelopeRolloverAsync(
        string month,
        CancellationToken cancellationToken = default);
}
