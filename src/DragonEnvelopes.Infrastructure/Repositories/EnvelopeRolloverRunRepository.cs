using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class EnvelopeRolloverRunRepository(DragonEnvelopesDbContext dbContext) : IEnvelopeRolloverRunRepository
{
    public Task<EnvelopeRolloverRun?> GetByFamilyAndMonthAsync(
        Guid familyId,
        string month,
        CancellationToken cancellationToken = default)
    {
        return dbContext.EnvelopeRolloverRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.FamilyId == familyId && x.Month == month,
                cancellationToken);
    }

    public async Task AddAsync(EnvelopeRolloverRun run, CancellationToken cancellationToken = default)
    {
        dbContext.EnvelopeRolloverRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
