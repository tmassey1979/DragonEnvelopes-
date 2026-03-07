using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IFamilyInviteRepository
{
    Task AddAsync(FamilyInvite invite, CancellationToken cancellationToken = default);
    Task AddTimelineEventAsync(FamilyInviteTimelineEvent timelineEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FamilyInvite>> ListByFamilyAsync(Guid familyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FamilyInviteTimelineEvent>> ListTimelineByFamilyAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<FamilyInvite?> GetByIdForUpdateAsync(Guid inviteId, CancellationToken cancellationToken = default);

    Task<FamilyInvite?> GetByTokenHashForUpdateAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<bool> HasPendingInviteAsync(Guid familyId, string email, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
