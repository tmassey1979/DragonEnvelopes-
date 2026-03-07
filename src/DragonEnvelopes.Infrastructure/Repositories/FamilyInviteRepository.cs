using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class FamilyInviteRepository(DragonEnvelopesDbContext dbContext) : IFamilyInviteRepository
{
    public Task AddAsync(FamilyInvite invite, CancellationToken cancellationToken = default)
    {
        dbContext.FamilyInvites.Add(invite);
        return Task.CompletedTask;
    }

    public Task AddTimelineEventAsync(
        FamilyInviteTimelineEvent timelineEvent,
        CancellationToken cancellationToken = default)
    {
        dbContext.FamilyInviteTimelineEvents.Add(timelineEvent);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<FamilyInvite>> ListByFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.FamilyInvites
            .Where(x => x.FamilyId == familyId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FamilyInviteTimelineEvent>> ListTimelineByFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.FamilyInviteTimelineEvents
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public Task<FamilyInvite?> GetByIdForUpdateAsync(
        Guid inviteId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.FamilyInvites
            .FirstOrDefaultAsync(x => x.Id == inviteId, cancellationToken);
    }

    public Task<FamilyInvite?> GetByTokenHashForUpdateAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return dbContext.FamilyInvites
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }

    public Task<bool> HasPendingInviteAsync(
        Guid familyId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = string.IsNullOrWhiteSpace(email)
            ? string.Empty
            : email.Trim().ToUpperInvariant();

        return dbContext.FamilyInvites
            .AsNoTracking()
            .AnyAsync(
                x => x.FamilyId == familyId
                    && x.Status == FamilyInviteStatus.Pending
                    && x.Email.ToUpper() == normalizedEmail,
                cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
