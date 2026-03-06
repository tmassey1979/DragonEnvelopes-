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

    public async Task AddAsync(StripeWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        await dbContext.StripeWebhookEvents.AddAsync(webhookEvent, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
