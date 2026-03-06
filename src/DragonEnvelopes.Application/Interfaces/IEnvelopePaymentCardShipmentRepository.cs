using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IEnvelopePaymentCardShipmentRepository
{
    Task<EnvelopePaymentCardShipment?> GetByCardIdAsync(Guid cardId, CancellationToken cancellationToken = default);

    Task<EnvelopePaymentCardShipment?> GetByCardIdForUpdateAsync(Guid cardId, CancellationToken cancellationToken = default);

    Task AddAsync(EnvelopePaymentCardShipment shipment, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
