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

    Task<FamilyBudgetPreferencesDetails?> GetBudgetPreferencesAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<FamilyBudgetPreferencesDetails> UpdateBudgetPreferencesAsync(
        Guid familyId,
        string payFrequency,
        string budgetingStyle,
        decimal? householdMonthlyIncome,
        CancellationToken cancellationToken = default);

    Task<FamilyMemberDetails> AddMemberAsync(
        Guid familyId,
        string keycloakUserId,
        string name,
        string email,
        string role,
        CancellationToken cancellationToken = default);

    Task<FamilyMemberDetails> UpdateMemberRoleAsync(
        Guid familyId,
        Guid memberId,
        string role,
        CancellationToken cancellationToken = default);

    Task RemoveMemberAsync(
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FamilyMemberDetails>?> ListMembersAsync(Guid familyId, CancellationToken cancellationToken = default);
}
