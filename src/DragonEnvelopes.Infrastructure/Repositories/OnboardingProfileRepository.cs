using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class OnboardingProfileRepository(DragonEnvelopesDbContext dbContext) : IOnboardingProfileRepository
{
    public Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return dbContext.Families
            .AsNoTracking()
            .AnyAsync(x => x.Id == familyId, cancellationToken);
    }

    public Task<OnboardingProfile?> GetByFamilyIdForUpdateAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.OnboardingProfiles
            .FirstOrDefaultAsync(x => x.FamilyId == familyId, cancellationToken);
    }

    public async Task AddAsync(OnboardingProfile profile, CancellationToken cancellationToken = default)
    {
        dbContext.OnboardingProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
