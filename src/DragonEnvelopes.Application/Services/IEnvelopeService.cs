using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IEnvelopeService
{
    Task<EnvelopeDetails> CreateAsync(
        Guid familyId,
        string name,
        decimal monthlyBudget,
        CancellationToken cancellationToken = default);

    Task<EnvelopeDetails?> GetByIdAsync(Guid envelopeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnvelopeDetails>> ListByFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default);

    Task<EnvelopeDetails> UpdateAsync(
        Guid envelopeId,
        string name,
        decimal monthlyBudget,
        bool isArchived,
        CancellationToken cancellationToken = default);

    Task<EnvelopeDetails> ArchiveAsync(Guid envelopeId, CancellationToken cancellationToken = default);
}
