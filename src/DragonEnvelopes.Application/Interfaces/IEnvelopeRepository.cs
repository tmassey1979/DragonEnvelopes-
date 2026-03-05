using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Interfaces;

public interface IEnvelopeRepository
{
    Task AddEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default);

    Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task<bool> EnvelopeNameExistsAsync(
        Guid familyId,
        string name,
        Guid? excludeEnvelopeId = null,
        CancellationToken cancellationToken = default);

    Task<Envelope?> GetByIdAsync(Guid envelopeId, CancellationToken cancellationToken = default);

    Task<Envelope?> GetByIdForUpdateAsync(Guid envelopeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Envelope>> ListByFamilyAsync(Guid familyId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
