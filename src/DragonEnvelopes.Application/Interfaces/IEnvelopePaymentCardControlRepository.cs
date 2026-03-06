using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IEnvelopePaymentCardControlRepository
{
    Task<EnvelopePaymentCardControl?> GetByCardIdAsync(Guid cardId, CancellationToken cancellationToken = default);

    Task<EnvelopePaymentCardControl?> GetByCardIdForUpdateAsync(Guid cardId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnvelopePaymentCardControlAudit>> ListAuditByCardIdAsync(Guid cardId, CancellationToken cancellationToken = default);

    Task AddAsync(EnvelopePaymentCardControl control, CancellationToken cancellationToken = default);

    Task AddAuditAsync(EnvelopePaymentCardControlAudit auditEntry, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
