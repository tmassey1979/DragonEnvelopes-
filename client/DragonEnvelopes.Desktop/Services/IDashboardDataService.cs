namespace DragonEnvelopes.Desktop.Services;

public interface IDashboardDataService
{
    Task<DashboardWorkspaceData> GetWorkspaceAsync(CancellationToken cancellationToken = default);
}
