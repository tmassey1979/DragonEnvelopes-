namespace DragonEnvelopes.Desktop.Services;

public interface IReportsDataService
{
    Task<ReportWorkspaceData> GetWorkspaceAsync(
        string month,
        DateTimeOffset from,
        DateTimeOffset to,
        bool includeArchived,
        CancellationToken cancellationToken = default);
}
