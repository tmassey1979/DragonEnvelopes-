using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IEnvelopeService
{
    Task<EnvelopeDetails> CreateAsync(
        Guid familyId,
        string name,
        decimal monthlyBudget,
        string? rolloverMode = null,
        decimal? rolloverCap = null,
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
        string? rolloverMode = null,
        decimal? rolloverCap = null,
        CancellationToken cancellationToken = default);

    Task<EnvelopeDetails> ArchiveAsync(Guid envelopeId, CancellationToken cancellationToken = default);

    Task<EnvelopeDetails> UpdateRolloverPolicyAsync(
        Guid envelopeId,
        string rolloverMode,
        decimal? rolloverCap,
        CancellationToken cancellationToken = default);
}
