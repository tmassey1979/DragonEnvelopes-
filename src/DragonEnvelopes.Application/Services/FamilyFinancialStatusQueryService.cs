using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Services;

public sealed class FamilyFinancialStatusQueryService(
    IFamilyFinancialProfileRepository financialProfileRepository) : IFamilyFinancialStatusQueryService
{
    public async Task<FamilyFinancialProfileDetails> GetStatusAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        if (!await financialProfileRepository.FamilyExistsAsync(familyId, cancellationToken))
        {
            throw new DomainValidationException("Family was not found.");
        }

        var profile = await financialProfileRepository.GetByFamilyIdAsync(familyId, cancellationToken);
        return profile is null
            ? new FamilyFinancialProfileDetails(
                Guid.Empty,
                familyId,
                PlaidConnected: false,
                PlaidItemId: null,
                StripeConnected: false,
                StripeCustomerId: null,
                UpdatedAtUtc: null,
                ReconciliationDriftThreshold: FamilyFinancialProfile.DefaultReconciliationDriftThreshold)
            : new FamilyFinancialProfileDetails(
                profile.Id,
                profile.FamilyId,
                profile.PlaidConnected,
                profile.PlaidItemId,
                profile.StripeConnected,
                profile.StripeCustomerId,
                profile.UpdatedAtUtc,
                profile.ReconciliationDriftThreshold);
    }
}
