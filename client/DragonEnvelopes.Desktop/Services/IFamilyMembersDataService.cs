using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public interface IFamilyMembersDataService
{
    Task<IReadOnlyList<FamilyMemberItemViewModel>> GetMembersAsync(CancellationToken cancellationToken = default);

    Task<FamilyMemberItemViewModel> AddMemberAsync(
        string keycloakUserId,
        string name,
        string email,
        string role,
        CancellationToken cancellationToken = default);

    Task<FamilyMemberItemViewModel> UpdateMemberRoleAsync(
        Guid memberId,
        string role,
        CancellationToken cancellationToken = default);

    Task RemoveMemberAsync(
        Guid memberId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FamilyInviteItemViewModel>> GetInvitesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FamilyInviteTimelineItemViewModel>> GetInviteTimelineAsync(
        string? emailFilter = null,
        string? eventTypeFilter = null,
        int take = 200,
        CancellationToken cancellationToken = default);

    Task<CreateFamilyInviteResultData> CreateInviteAsync(
        string email,
        string role,
        int expiresInHours,
        CancellationToken cancellationToken = default);

    Task<FamilyInviteItemViewModel> CancelInviteAsync(
        Guid inviteId,
        CancellationToken cancellationToken = default);

    Task<CreateFamilyInviteResultData> ResendInviteAsync(
        Guid inviteId,
        int expiresInHours,
        CancellationToken cancellationToken = default);
}
