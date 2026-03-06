using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IFamilyService
{
    Task<FamilyDetails> CreateAsync(string name, CancellationToken cancellationToken = default);

    Task<FamilyDetails?> GetByIdAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<FamilyProfileDetails?> GetProfileAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<FamilyProfileDetails> UpdateProfileAsync(
        Guid familyId,
        string name,
        string currencyCode,
        string timeZoneId,
        CancellationToken cancellationToken = default);

    Task<FamilyMemberDetails> AddMemberAsync(
        Guid familyId,
        string keycloakUserId,
        string name,
        string email,
        string role,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FamilyMemberDetails>?> ListMembersAsync(Guid familyId, CancellationToken cancellationToken = default);
}
