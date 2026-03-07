using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class FamilyRepository(DragonEnvelopesDbContext dbContext) : IFamilyRepository
{
    public async Task AddFamilyAsync(Family family, CancellationToken cancellationToken = default)
    {
        dbContext.Families.Add(family);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddMemberAsync(FamilyMember member, CancellationToken cancellationToken = default)
    {
        dbContext.FamilyMembers.Add(member);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Family?> GetFamilyByIdAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return dbContext.Families
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == familyId, cancellationToken);
    }

    public Task<Family?> GetFamilyByIdForUpdateAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return dbContext.Families
            .FirstOrDefaultAsync(x => x.Id == familyId, cancellationToken);
    }

    public Task<FamilyMember?> GetMemberByIdForUpdateAsync(
        Guid familyId,
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.FamilyMembers
            .FirstOrDefaultAsync(
                x => x.FamilyId == familyId && x.Id == memberId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<FamilyMember>> ListMembersAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return await dbContext.FamilyMembers
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId)
            .OrderBy(x => x.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountMembersByRoleAsync(
        Guid familyId,
        MemberRole role,
        CancellationToken cancellationToken = default)
    {
        return dbContext.FamilyMembers
            .AsNoTracking()
            .CountAsync(
                x => x.FamilyId == familyId && x.Role == role,
                cancellationToken);
    }

    public Task<bool> FamilyNameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = string.IsNullOrWhiteSpace(name)
            ? string.Empty
            : name.Trim().ToUpperInvariant();

        return dbContext.Families
            .AsNoTracking()
            .AnyAsync(
                x => x.Name.ToUpper() == normalizedName,
                cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task RemoveMemberAsync(FamilyMember member, CancellationToken cancellationToken = default)
    {
        dbContext.FamilyMembers.Remove(member);
        return Task.CompletedTask;
    }

    public Task<bool> MemberKeycloakUserIdExistsAsync(
        Guid familyId,
        string keycloakUserId,
        CancellationToken cancellationToken = default)
    {
        var normalizedKeycloakUserId = string.IsNullOrWhiteSpace(keycloakUserId)
            ? string.Empty
            : keycloakUserId.Trim().ToUpperInvariant();

        return dbContext.FamilyMembers
            .AsNoTracking()
            .AnyAsync(
                x => x.FamilyId == familyId
                    && x.KeycloakUserId.ToUpper() == normalizedKeycloakUserId,
                cancellationToken);
    }
}
