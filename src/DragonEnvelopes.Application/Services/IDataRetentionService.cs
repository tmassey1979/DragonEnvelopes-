namespace DragonEnvelopes.Application.Services;

public interface IDataRetentionService
{
    Task<DataRetentionCleanupResult> CleanupAsync(CancellationToken cancellationToken = default);
}
