using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
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

    public async Task<IReadOnlyList<FamilyMember>> ListMembersAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return await dbContext.FamilyMembers
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId)
            .OrderBy(x => x.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> FamilyNameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return dbContext.Families
            .AsNoTracking()
            .AnyAsync(x => EF.Functions.ILike(x.Name, name), cancellationToken);
    }

    public Task<bool> MemberKeycloakUserIdExistsAsync(
        Guid familyId,
        string keycloakUserId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.FamilyMembers
            .AsNoTracking()
            .AnyAsync(
                x => x.FamilyId == familyId
                    && EF.Functions.ILike(x.KeycloakUserId, keycloakUserId),
                cancellationToken);
    }
}
