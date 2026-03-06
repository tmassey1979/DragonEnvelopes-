using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IEnvelopePaymentCardService
{
    Task<EnvelopePaymentCardDetails> IssueVirtualCardAsync(
        Guid familyId,
        Guid envelopeId,
        string? cardholderName,
        CancellationToken cancellationToken = default);

    Task<EnvelopePhysicalCardIssuanceDetails> IssuePhysicalCardAsync(
        Guid familyId,
        Guid envelopeId,
        string? cardholderName,
        string recipientName,
        string addressLine1,
        string? addressLine2,
        string city,
        string stateOrProvince,
        string postalCode,
        string countryCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnvelopePaymentCardDetails>> ListByEnvelopeAsync(
        Guid familyId,
        Guid envelopeId,
        CancellationToken cancellationToken = default);

    Task<EnvelopePhysicalCardIssuanceDetails?> GetPhysicalCardIssuanceAsync(
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default);

    Task<EnvelopePhysicalCardIssuanceDetails> RefreshPhysicalCardIssuanceStatusAsync(
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
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
