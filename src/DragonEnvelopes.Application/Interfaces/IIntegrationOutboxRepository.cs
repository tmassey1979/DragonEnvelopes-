using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IIntegrationOutboxRepository
{
    Task AddAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IntegrationOutboxMessage>> ListDispatchableAsync(
        DateTimeOffset nowUtc,
        int take,
        string sourceService,
        CancellationToken cancellationToken = default);

    Task<int> CountPendingAsync(string sourceService, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
