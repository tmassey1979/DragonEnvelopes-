using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;

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

    private static FamilyDetails Map(Family family, IReadOnlyList<FamilyMember> members)
    {
        return new FamilyDetails(
            family.Id,
            family.Name,
            family.CreatedAt,
            members.Select(static member => new FamilyMemberDetails(
                    member.Id,
                    member.FamilyId,
                    member.KeycloakUserId,
                    member.Name,
                    member.Email.Value,
                    member.Role.ToString()))
                .ToArray());
    }
}
