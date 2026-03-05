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
    public async Task<FamilyDetails> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = string.IsNullOrWhiteSpace(name)
            ? string.Empty
            : name.Trim();

        if (await familyRepository.FamilyNameExistsAsync(normalizedName, cancellationToken))
        {
            throw new DomainValidationException("A family with the same name already exists.");
        }

        var family = new Family(Guid.NewGuid(), normalizedName, clock.UtcNow);
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
