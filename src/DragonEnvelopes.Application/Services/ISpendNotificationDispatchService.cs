using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface ISpendNotificationDispatchService
{
    Task<SpendNotificationDispatchResult> DispatchPendingAsync(CancellationToken cancellationToken = default);
}
