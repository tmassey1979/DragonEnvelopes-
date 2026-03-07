using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IFamilyInviteService
{
    Task<CreateFamilyInviteResult> CreateAsync(
        Guid familyId,
        string email,
        string role,
        int expiresInHours,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FamilyInviteDetails>> ListByFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default);

    Task<FamilyInviteDetails> CancelAsync(
        Guid inviteId,
        CancellationToken cancellationToken = default);

    Task<FamilyInviteDetails> AcceptAsync(
        string inviteToken,
        CancellationToken cancellationToken = default);

    Task<FamilyInviteRedemptionDetails> RedeemAsync(
        string inviteToken,
        string keycloakUserId,
        string? memberName,
        string? memberEmail,
        CancellationToken cancellationToken = default);
}
