using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class EnvelopePaymentCardRepository(DragonEnvelopesDbContext dbContext) : IEnvelopePaymentCardRepository
{
    public Task<EnvelopePaymentCard?> GetByIdAsync(Guid cardId, CancellationToken cancellationToken = default)
    {
        return dbContext.EnvelopePaymentCards
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == cardId, cancellationToken);
    }

    public Task<EnvelopePaymentCard?> GetByIdForUpdateAsync(Guid cardId, CancellationToken cancellationToken = default)
    {
        return dbContext.EnvelopePaymentCards
            .FirstOrDefaultAsync(x => x.Id == cardId, cancellationToken);
    }

    public async Task<IReadOnlyList<EnvelopePaymentCard>> ListByEnvelopeAsync(Guid envelopeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.EnvelopePaymentCards
            .AsNoTracking()
            .Where(x => x.EnvelopeId == envelopeId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(EnvelopePaymentCard card, CancellationToken cancellationToken = default)
    {
        dbContext.EnvelopePaymentCards.Add(card);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
