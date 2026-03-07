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

    public Task<EnvelopePaymentCard?> GetByProviderCardIdAsync(
        string provider,
        string providerCardId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.EnvelopePaymentCards
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Provider == provider && x.ProviderCardId == providerCardId,
                cancellationToken);
    }

    public Task<EnvelopePaymentCard?> GetByProviderCardIdForUpdateAsync(
        string provider,
        string providerCardId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.EnvelopePaymentCards
            .FirstOrDefaultAsync(
                x => x.Provider == provider && x.ProviderCardId == providerCardId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<EnvelopePaymentCard>> ListByEnvelopeAsync(Guid envelopeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.EnvelopePaymentCards
            .AsNoTracking()
            .Where(x => x.EnvelopeId == envelopeId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public Task AddAsync(EnvelopePaymentCard card, CancellationToken cancellationToken = default)
    {
        dbContext.EnvelopePaymentCards.Add(card);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
