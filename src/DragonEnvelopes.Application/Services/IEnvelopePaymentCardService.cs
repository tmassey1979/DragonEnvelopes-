using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IEnvelopePaymentCardService
{
    Task<EnvelopePaymentCardDetails> IssueVirtualCardAsync(
        Guid familyId,
        Guid envelopeId,
        string? cardholderName,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnvelopePaymentCardDetails>> ListByEnvelopeAsync(
        Guid familyId,
        Guid envelopeId,
        CancellationToken cancellationToken = default);

    Task<EnvelopePaymentCardDetails> FreezeCardAsync(
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default);

    Task<EnvelopePaymentCardDetails> UnfreezeCardAsync(
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default);

    Task<EnvelopePaymentCardDetails> CancelCardAsync(
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default);
}
