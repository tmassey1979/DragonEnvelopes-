namespace DragonEnvelopes.Desktop.Services;

public interface IBudgetsDataService
{
    Task<BudgetAllocationWorkspaceData?> GetWorkspaceAsync(
        string month,
        bool includeArchived,
        CancellationToken cancellationToken = default);
}
