using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IFamilyInviteService
{
    Task<CreateFamilyInviteResult> CreateAsync(
        Guid familyId,
        string email,
        string role,
        int expiresInHours,
        string? actorUserId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FamilyInviteDetails>> ListByFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default);

    Task<FamilyInviteDetails> CancelAsync(
        Guid inviteId,
        string? actorUserId = null,
        CancellationToken cancellationToken = default);

    Task<CreateFamilyInviteResult> ResendAsync(
        Guid inviteId,
        int expiresInHours,
        string? actorUserId = null,
        CancellationToken cancellationToken = default);

    Task<FamilyInviteDetails> AcceptAsync(
        string inviteToken,
        string? actorUserId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FamilyInviteTimelineEventDetails>> ListTimelineByFamilyAsync(
        Guid familyId,
        string? emailFilter = null,
        string? eventTypeFilter = null,
        int take = 200,
        CancellationToken cancellationToken = default);

    Task<FamilyInviteRedemptionDetails> RedeemAsync(
        string inviteToken,
        string keycloakUserId,
        string? memberName,
        string? memberEmail,
        CancellationToken cancellationToken = default);
}
