using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IEnvelopeRolloverService
{
    Task<EnvelopeRolloverPreviewDetails> PreviewAsync(
        Guid familyId,
        string month,
        CancellationToken cancellationToken = default);

    Task<EnvelopeRolloverApplyDetails> ApplyAsync(
        Guid familyId,
        string month,
        string? appliedByUserId,
        CancellationToken cancellationToken = default);
}
