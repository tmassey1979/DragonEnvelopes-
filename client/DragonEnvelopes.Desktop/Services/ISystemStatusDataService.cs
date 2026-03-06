namespace DragonEnvelopes.Desktop.Services;

public interface ISystemStatusDataService
{
    Task<SystemRuntimeStatusData> GetRuntimeStatusAsync(CancellationToken cancellationToken = default);
}
