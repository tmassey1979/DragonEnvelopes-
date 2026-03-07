using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class IntegrationInboxRepository(DragonEnvelopesDbContext dbContext) : IIntegrationInboxRepository
{
    public async Task<IntegrationInboxMessage?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var normalizedIdempotencyKey = NormalizeRequired(idempotencyKey, "Inbox idempotency key");
        return await dbContext.IntegrationInboxMessages
            .SingleOrDefaultAsync(
                message => message.IdempotencyKey == normalizedIdempotencyKey,
                cancellationToken);
    }

    public Task AddAsync(IntegrationInboxMessage message, CancellationToken cancellationToken = default)
    {
        dbContext.IntegrationInboxMessages.Add(message);
        return Task.CompletedTask;
    }

    public Task<int> CountDeadLetteredAsync(
        string consumerName,
        CancellationToken cancellationToken = default)
    {
        var normalizedConsumerName = NormalizeRequired(consumerName, "Inbox consumer name");
        return dbContext.IntegrationInboxMessages
            .AsNoTracking()
            .CountAsync(
                message =>
                    message.ConsumerName == normalizedConsumerName
                    && message.DeadLetteredAtUtc != null,
                cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeRequired(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{field} is required.");
        }

        return value.Trim();
    }
}
