using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IFamilyRepository
{
    Task AddFamilyAsync(Family family, CancellationToken cancellationToken = default);

    Task AddMemberAsync(FamilyMember member, CancellationToken cancellationToken = default);

    Task<Family?> GetFamilyByIdAsync(Guid familyId, CancellationToken cancellationToken = default);
    Task<Family?> GetFamilyByIdForUpdateAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FamilyMember>> ListMembersAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<bool> FamilyNameExistsAsync(string name, CancellationToken cancellationToken = default);

    Task<bool> MemberKeycloakUserIdExistsAsync(
        Guid familyId,
        string keycloakUserId,
        CancellationToken cancellationToken = default);
}
