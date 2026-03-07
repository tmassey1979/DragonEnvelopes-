using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IIntegrationOutboxRepository
{
    Task AddAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IntegrationOutboxMessage>> ListDispatchableAsync(
        DateTimeOffset nowUtc,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountPendingAsync(CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
