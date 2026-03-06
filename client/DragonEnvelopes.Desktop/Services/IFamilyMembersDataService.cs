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

    Task<IReadOnlyList<FamilyInviteItemViewModel>> GetInvitesAsync(CancellationToken cancellationToken = default);

    Task<CreateFamilyInviteResultData> CreateInviteAsync(
        string email,
        string role,
        int expiresInHours,
        CancellationToken cancellationToken = default);

    Task<FamilyInviteItemViewModel> CancelInviteAsync(
        Guid inviteId,
        CancellationToken cancellationToken = default);
}
