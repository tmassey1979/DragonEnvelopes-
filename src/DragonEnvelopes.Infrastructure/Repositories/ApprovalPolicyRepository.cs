using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class ApprovalPolicyRepository(DragonEnvelopesDbContext dbContext) : IApprovalPolicyRepository
{
    public Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return dbContext.Families
            .AsNoTracking()
            .AnyAsync(x => x.Id == familyId, cancellationToken);
    }

    public Task<FamilyApprovalPolicy?> GetByFamilyIdAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.FamilyApprovalPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.FamilyId == familyId, cancellationToken);
    }

    public Task<FamilyApprovalPolicy?> GetByFamilyIdForUpdateAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.FamilyApprovalPolicies
            .FirstOrDefaultAsync(x => x.FamilyId == familyId, cancellationToken);
    }

    public async Task AddAsync(
        FamilyApprovalPolicy policy,
        CancellationToken cancellationToken = default)
    {
        await dbContext.FamilyApprovalPolicies.AddAsync(policy, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
