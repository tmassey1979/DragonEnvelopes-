using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IReportingProjectionService
{
    Task<ReportingProjectionBatchDetails> ProjectPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<ReportingProjectionReplayDetails> ReplayAsync(
        ReportingProjectionReplayRequestDetails request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReportingProjectionReplayRunDetails>> ListReplayRunsAsync(
        Guid? familyId,
        int take,
        CancellationToken cancellationToken = default);

    Task<ReportingProjectionReplayRunDetails?> GetReplayRunAsync(
        Guid replayRunId,
        CancellationToken cancellationToken = default);

    Task<ReportingProjectionStatusDetails> GetStatusAsync(
        Guid? familyId,
        CancellationToken cancellationToken = default);
}
