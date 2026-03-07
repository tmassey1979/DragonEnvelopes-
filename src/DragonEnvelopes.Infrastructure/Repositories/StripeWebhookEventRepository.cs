using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class StripeWebhookEventRepository(DragonEnvelopesDbContext dbContext) : IStripeWebhookEventRepository
{
    public Task<StripeWebhookEvent?> GetByEventIdAsync(string eventId, CancellationToken cancellationToken = default)
    {
        return dbContext.StripeWebhookEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EventId == eventId, cancellationToken);
    }

    public Task<StripeWebhookEvent?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.StripeWebhookEvents
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(StripeWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        await dbContext.StripeWebhookEvents.AddAsync(webhookEvent, cancellationToken);
    }

    public async Task<int> DeleteProcessedBeforeAsync(
        DateTimeOffset cutoffUtc,
        int take,
        CancellationToken cancellationToken = default)
    {
        var normalizedTake = take <= 0 ? 100 : take;
        var ids = await dbContext.StripeWebhookEvents
            .Where(x => x.ProcessedAtUtc < cutoffUtc)
            .OrderBy(x => x.ProcessedAtUtc)
            .Take(normalizedTake)
            .Select(x => x.Id)
            .ToArrayAsync(cancellationToken);
        if (ids.Length == 0)
        {
            return 0;
        }

        var entities = await dbContext.StripeWebhookEvents
            .Where(x => ids.Contains(x.Id))
            .ToArrayAsync(cancellationToken);

        dbContext.StripeWebhookEvents.RemoveRange(entities);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entities.Length;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
