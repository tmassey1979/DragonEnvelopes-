using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IReportingProjectionService
{
    Task<ReportingProjectionBatchDetails> ProjectPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<ReportingProjectionReplayDetails> ReplayAsync(
        Guid? familyId,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<ReportingProjectionStatusDetails> GetStatusAsync(
        Guid? familyId,
        CancellationToken cancellationToken = default);
}
