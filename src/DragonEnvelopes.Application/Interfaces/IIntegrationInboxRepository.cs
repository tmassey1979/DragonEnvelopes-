using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IIntegrationInboxRepository
{
    Task<IntegrationInboxMessage?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task AddAsync(IntegrationInboxMessage message, CancellationToken cancellationToken = default);

    Task<int> CountDeadLetteredAsync(
        string consumerName,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
