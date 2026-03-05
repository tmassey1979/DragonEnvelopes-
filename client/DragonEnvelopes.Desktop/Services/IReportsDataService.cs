namespace DragonEnvelopes.Desktop.Services;

public interface IReportsDataService
{
    Task<ReportSummaryData?> GetSummaryAsync(
        string month,
        bool includeArchived,
        CancellationToken cancellationToken = default);
}
