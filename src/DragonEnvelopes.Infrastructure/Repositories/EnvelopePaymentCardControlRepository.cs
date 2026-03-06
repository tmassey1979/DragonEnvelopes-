using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class EnvelopePaymentCardControlRepository(DragonEnvelopesDbContext dbContext) : IEnvelopePaymentCardControlRepository
{
    public Task<EnvelopePaymentCardControl?> GetByCardIdAsync(Guid cardId, CancellationToken cancellationToken = default)
    {
        return dbContext.EnvelopePaymentCardControls
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CardId == cardId, cancellationToken);
    }

    public Task<EnvelopePaymentCardControl?> GetByCardIdForUpdateAsync(Guid cardId, CancellationToken cancellationToken = default)
    {
        return dbContext.EnvelopePaymentCardControls
            .FirstOrDefaultAsync(x => x.CardId == cardId, cancellationToken);
    }

    public async Task<IReadOnlyList<EnvelopePaymentCardControlAudit>> ListAuditByCardIdAsync(Guid cardId, CancellationToken cancellationToken = default)
    {
        return await dbContext.EnvelopePaymentCardControlAudits
            .AsNoTracking()
            .Where(x => x.CardId == cardId)
            .OrderByDescending(x => x.ChangedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(EnvelopePaymentCardControl control, CancellationToken cancellationToken = default)
    {
        await dbContext.EnvelopePaymentCardControls.AddAsync(control, cancellationToken);
    }

    public async Task AddAuditAsync(EnvelopePaymentCardControlAudit auditEntry, CancellationToken cancellationToken = default)
    {
        await dbContext.EnvelopePaymentCardControlAudits.AddAsync(auditEntry, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
