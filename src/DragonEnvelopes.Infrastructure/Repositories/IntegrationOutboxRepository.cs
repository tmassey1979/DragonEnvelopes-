using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class IntegrationOutboxRepository(DragonEnvelopesDbContext dbContext) : IIntegrationOutboxRepository
{
    public Task AddAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken = default)
    {
        dbContext.IntegrationOutboxMessages.Add(message);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<IntegrationOutboxMessage>> ListDispatchableAsync(
        DateTimeOffset nowUtc,
        int take,
        CancellationToken cancellationToken = default)
    {
        var normalizedTake = take <= 0 ? 50 : take;
        return await dbContext.IntegrationOutboxMessages
            .Where(message =>
                message.DispatchedAtUtc == null
                && message.NextAttemptAtUtc <= nowUtc)
            .OrderBy(message => message.CreatedAtUtc)
            .ThenBy(message => message.Id)
            .Take(normalizedTake)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountPendingAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.IntegrationOutboxMessages
            .AsNoTracking()
            .CountAsync(message => message.DispatchedAtUtc == null, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
