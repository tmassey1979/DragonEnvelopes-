using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IEnvelopeFinancialAccountService
{
    Task<EnvelopeFinancialAccountDetails> LinkStripeFinancialAccountAsync(
        Guid familyId,
        Guid envelopeId,
        string? displayName,
        CancellationToken cancellationToken = default);

    Task<EnvelopeFinancialAccountDetails?> GetByEnvelopeAsync(
        Guid familyId,
        Guid envelopeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnvelopeFinancialAccountDetails>> ListByFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default);
}
