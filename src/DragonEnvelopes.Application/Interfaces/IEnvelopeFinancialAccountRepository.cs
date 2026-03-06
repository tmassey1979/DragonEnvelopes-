using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IEnvelopeFinancialAccountRepository
{
    Task<EnvelopeFinancialAccount?> GetByEnvelopeIdAsync(Guid envelopeId, CancellationToken cancellationToken = default);

    Task<EnvelopeFinancialAccount?> GetByEnvelopeIdForUpdateAsync(Guid envelopeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnvelopeFinancialAccount>> ListByFamilyAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task AddAsync(EnvelopeFinancialAccount account, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
