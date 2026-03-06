using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class FamilyService(
    IFamilyRepository familyRepository,
    IClock clock) : IFamilyService
{
    private const string DefaultCurrencyCode = Family.DefaultCurrencyCode;
    private const string DefaultTimeZoneId = Family.DefaultTimeZoneId;

    public async Task<FamilyDetails> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = string.IsNullOrWhiteSpace(name)
            ? string.Empty
            : name.Trim();

        if (await familyRepository.FamilyNameExistsAsync(normalizedName, cancellationToken))
        {
            throw new DomainValidationException("A family with the same name already exists.");
        }

        var family = new Family(
            Guid.NewGuid(),
            normalizedName,
            clock.UtcNow,
            DefaultCurrencyCode,
            DefaultTimeZoneId,
            clock.UtcNow);
        await familyRepository.AddFamilyAsync(family, cancellationToken);
        return Map(family, []);
    }

    public async Task<FamilyDetails?> GetByIdAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        var family = await familyRepository.GetFamilyByIdAsync(familyId, cancellationToken);
        if (family is null)
        {
            return null;
        }

        var members = await familyRepository.ListMembersAsync(familyId, cancellationToken);
        return Map(family, members);
    }

    public async Task<FamilyProfileDetails?> GetProfileAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        var family = await familyRepository.GetFamilyByIdAsync(familyId, cancellationToken);
        if (family is null)
        {
            return null;
        }

        return MapProfile(family);
    }

    public async Task<FamilyProfileDetails> UpdateProfileAsync(
        Guid familyId,
        string name,
        string currencyCode,
        string timeZoneId,
        CancellationToken cancellationToken = default)
    {
        var family = await familyRepository.GetFamilyByIdForUpdateAsync(familyId, cancellationToken);
        if (family is null)
        {
            throw new DomainValidationException("Family was not found.");
        }

        family.UpdateProfile(name, currencyCode, timeZoneId, clock.UtcNow);
        await familyRepository.SaveChangesAsync(cancellationToken);

        return MapProfile(family);
    }

    public async Task<FamilyBudgetPreferencesDetails?> GetBudgetPreferencesAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        var family = await familyRepository.GetFamilyByIdAsync(familyId, cancellationToken);
        if (family is null)
        {
            return null;
        }

        return MapBudgetPreferences(family);
    }

    public async Task<FamilyBudgetPreferencesDetails> UpdateBudgetPreferencesAsync(
        Guid familyId,
        string payFrequency,
        string budgetingStyle,
        decimal? householdMonthlyIncome,
        CancellationToken cancellationToken = default)
    {
        var family = await familyRepository.GetFamilyByIdForUpdateAsync(familyId, cancellationToken);
        if (family is null)
        {
            throw new DomainValidationException("Family was not found.");
        }

        family.UpdateBudgetPreferences(
            payFrequency,
            budgetingStyle,
            householdMonthlyIncome,
            clock.UtcNow);
        await familyRepository.SaveChangesAsync(cancellationToken);

        return MapBudgetPreferences(family);
    }

    public async Task<FamilyMemberDetails> AddMemberAsync(
        Guid familyId,
        string keycloakUserId,
        string name,
        string email,
        string role,
        CancellationToken cancellationToken = default)
    {
        var family = await familyRepository.GetFamilyByIdAsync(familyId, cancellationToken);
        if (family is null)
        {
            throw new DomainValidationException("Family was not found.");
        }

        var normalizedKeycloakUserId = string.IsNullOrWhiteSpace(keycloakUserId)
            ? string.Empty
            : keycloakUserId.Trim();

        if (await familyRepository.MemberKeycloakUserIdExistsAsync(familyId, normalizedKeycloakUserId, cancellationToken))
        {
            throw new DomainValidationException("A member with the same Keycloak user id already exists.");
        }

        if (!Enum.TryParse<MemberRole>(role, ignoreCase: true, out var parsedRole))
        {
            throw new DomainValidationException("Member role is invalid.");
        }

        var member = new FamilyMember(
            Guid.NewGuid(),
            familyId,
            keycloakUserId,
            name,
            EmailAddress.Parse(email),
            parsedRole);

        await familyRepository.AddMemberAsync(member, cancellationToken);
        return MapMember(member);
    }

    public async Task<IReadOnlyList<FamilyMemberDetails>?> ListMembersAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        var family = await familyRepository.GetFamilyByIdAsync(familyId, cancellationToken);
        if (family is null)
        {
            return null;
        }

        var members = await familyRepository.ListMembersAsync(familyId, cancellationToken);
        return members.Select(MapMember).ToArray();
    }

    private static FamilyDetails Map(Family family, IReadOnlyList<FamilyMember> members)
    {
        return new FamilyDetails(
            family.Id,
            family.Name,
            family.CreatedAt,
            members.Select(MapMember)
                .ToArray());
    }

    private static FamilyProfileDetails MapProfile(Family family)
    {
        return new FamilyProfileDetails(
            family.Id,
            family.Name,
            family.CurrencyCode,
            family.TimeZoneId,
            family.CreatedAt,
            family.UpdatedAt);
    }

    private static FamilyBudgetPreferencesDetails MapBudgetPreferences(Family family)
    {
        return new FamilyBudgetPreferencesDetails(
            family.Id,
            family.PayFrequency,
            family.BudgetingStyle,
            family.HouseholdMonthlyIncome,
            family.UpdatedAt);
    }

    private static FamilyMemberDetails MapMember(FamilyMember member)
    {
        return new FamilyMemberDetails(
            member.Id,
            member.FamilyId,
            member.KeycloakUserId,
            member.Name,
            member.Email.Value,
            member.Role.ToString());
    }
}
