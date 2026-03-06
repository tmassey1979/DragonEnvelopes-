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
}
