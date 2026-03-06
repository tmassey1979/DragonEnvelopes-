using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class EnvelopePaymentCardShipmentRepository(DragonEnvelopesDbContext dbContext) : IEnvelopePaymentCardShipmentRepository
{
    public Task<EnvelopePaymentCardShipment?> GetByCardIdAsync(Guid cardId, CancellationToken cancellationToken = default)
    {
        return dbContext.EnvelopePaymentCardShipments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CardId == cardId, cancellationToken);
    }

    public Task<EnvelopePaymentCardShipment?> GetByCardIdForUpdateAsync(Guid cardId, CancellationToken cancellationToken = default)
    {
        return dbContext.EnvelopePaymentCardShipments
            .FirstOrDefaultAsync(x => x.CardId == cardId, cancellationToken);
    }

    public async Task AddAsync(EnvelopePaymentCardShipment shipment, CancellationToken cancellationToken = default)
    {
        dbContext.EnvelopePaymentCardShipments.Add(shipment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
