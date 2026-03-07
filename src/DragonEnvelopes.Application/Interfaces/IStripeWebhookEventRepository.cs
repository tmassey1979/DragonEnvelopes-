using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IStripeWebhookEventRepository
{
    Task<StripeWebhookEvent?> GetByEventIdAsync(string eventId, CancellationToken cancellationToken = default);

    Task<StripeWebhookEvent?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(StripeWebhookEvent webhookEvent, CancellationToken cancellationToken = default);

    Task<int> DeleteProcessedBeforeAsync(
        DateTimeOffset cutoffUtc,
        int take,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
