using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IEnvelopePaymentCardRepository
{
    Task<EnvelopePaymentCard?> GetByIdAsync(Guid cardId, CancellationToken cancellationToken = default);

    Task<EnvelopePaymentCard?> GetByIdForUpdateAsync(Guid cardId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnvelopePaymentCard>> ListByEnvelopeAsync(Guid envelopeId, CancellationToken cancellationToken = default);

    Task AddAsync(EnvelopePaymentCard card, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
