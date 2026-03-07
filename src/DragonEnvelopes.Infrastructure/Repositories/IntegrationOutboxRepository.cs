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
        string sourceService,
        CancellationToken cancellationToken = default)
    {
        var normalizedSourceService = NormalizeSourceService(sourceService);
        var normalizedTake = take <= 0 ? 50 : take;
        return await dbContext.IntegrationOutboxMessages
            .Where(message =>
                message.SourceService == normalizedSourceService
                && 
                message.DispatchedAtUtc == null
                && message.NextAttemptAtUtc <= nowUtc)
            .OrderBy(message => message.CreatedAtUtc)
            .ThenBy(message => message.Id)
            .Take(normalizedTake)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountPendingAsync(string sourceService, CancellationToken cancellationToken = default)
    {
        var normalizedSourceService = NormalizeSourceService(sourceService);
        return dbContext.IntegrationOutboxMessages
            .AsNoTracking()
            .CountAsync(
                message =>
                    message.SourceService == normalizedSourceService
                    && message.DispatchedAtUtc == null,
                cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeSourceService(string sourceService)
    {
        if (string.IsNullOrWhiteSpace(sourceService))
        {
            throw new InvalidOperationException("Outbox source service is required.");
        }

        return sourceService.Trim();
    }
}
