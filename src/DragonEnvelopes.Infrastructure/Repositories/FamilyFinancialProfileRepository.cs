using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Infrastructure.Repositories;

public sealed class FamilyFinancialProfileRepository(
    DragonEnvelopesDbContext dbContext,
    IProviderSecretProtector providerSecretProtector) : IFamilyFinancialProfileRepository
{
    public Task<bool> FamilyExistsAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return dbContext.Families
            .AsNoTracking()
            .AnyAsync(x => x.Id == familyId, cancellationToken);
    }

    public async Task<FamilyFinancialProfile?> GetByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.FamilyFinancialProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.FamilyId == familyId, cancellationToken);
        return profile is null
            ? null
            : DecryptCopy(profile);
    }

    public async Task<FamilyFinancialProfile?> GetByFamilyIdForUpdateAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.FamilyFinancialProfiles
            .FirstOrDefaultAsync(x => x.FamilyId == familyId, cancellationToken);
        if (profile is not null)
        {
            DecryptInPlace(profile);
        }

        return profile;
    }

    public async Task<IReadOnlyList<FamilyFinancialProfile>> ListPlaidConnectedAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await dbContext.FamilyFinancialProfiles
            .AsNoTracking()
            .Where(x => x.PlaidItemId != null && x.PlaidAccessToken != null)
            .ToArrayAsync(cancellationToken);

        return profiles.Select(DecryptCopy).ToArray();
    }

    public async Task AddAsync(FamilyFinancialProfile profile, CancellationToken cancellationToken = default)
    {
        EncryptInPlace(profile);
        dbContext.FamilyFinancialProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EncryptTrackedProfiles();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void EncryptTrackedProfiles()
    {
        foreach (var entry in dbContext.ChangeTracker.Entries<FamilyFinancialProfile>())
        {
            if (entry.State is EntityState.Detached or EntityState.Deleted)
            {
                continue;
            }

            EncryptInPlace(entry.Entity);
        }
    }

    private FamilyFinancialProfile DecryptCopy(FamilyFinancialProfile profile)
    {
        return new FamilyFinancialProfile(
            profile.Id,
            profile.FamilyId,
            profile.PlaidItemId,
            DecryptNullable(profile.PlaidAccessToken),
            DecryptNullable(profile.StripeCustomerId),
            DecryptNullable(profile.StripeDefaultPaymentMethodId),
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc,
            profile.ReconciliationDriftThreshold);
    }

    private void DecryptInPlace(FamilyFinancialProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.PlaidItemId) && !string.IsNullOrWhiteSpace(profile.PlaidAccessToken))
        {
            var decryptedToken = providerSecretProtector.Unprotect(profile.PlaidAccessToken);
            if (!decryptedToken.Equals(profile.PlaidAccessToken, StringComparison.Ordinal))
            {
                profile.SetPlaidConnection(profile.PlaidItemId, decryptedToken, profile.UpdatedAtUtc);
            }
        }

        if (!string.IsNullOrWhiteSpace(profile.StripeCustomerId))
        {
            var decryptedCustomerId = providerSecretProtector.Unprotect(profile.StripeCustomerId);
            if (!decryptedCustomerId.Equals(profile.StripeCustomerId, StringComparison.Ordinal))
            {
                profile.SetStripeCustomer(decryptedCustomerId, profile.UpdatedAtUtc);
            }
        }

        if (!string.IsNullOrWhiteSpace(profile.StripeDefaultPaymentMethodId))
        {
            var decryptedPaymentMethodId = providerSecretProtector.Unprotect(profile.StripeDefaultPaymentMethodId);
            if (!decryptedPaymentMethodId.Equals(profile.StripeDefaultPaymentMethodId, StringComparison.Ordinal))
            {
                profile.SetStripeDefaultPaymentMethod(decryptedPaymentMethodId, profile.UpdatedAtUtc);
            }
        }
    }

    private void EncryptInPlace(FamilyFinancialProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.PlaidItemId) && !string.IsNullOrWhiteSpace(profile.PlaidAccessToken))
        {
            var protectedToken = providerSecretProtector.Protect(profile.PlaidAccessToken);
            if (!protectedToken.Equals(profile.PlaidAccessToken, StringComparison.Ordinal))
            {
                profile.SetPlaidConnection(profile.PlaidItemId, protectedToken, profile.UpdatedAtUtc);
            }
        }

        if (!string.IsNullOrWhiteSpace(profile.StripeCustomerId))
        {
            var protectedCustomerId = providerSecretProtector.Protect(profile.StripeCustomerId);
            if (!protectedCustomerId.Equals(profile.StripeCustomerId, StringComparison.Ordinal))
            {
                profile.SetStripeCustomer(protectedCustomerId, profile.UpdatedAtUtc);
            }
        }

        if (!string.IsNullOrWhiteSpace(profile.StripeDefaultPaymentMethodId))
        {
            var protectedPaymentMethodId = providerSecretProtector.Protect(profile.StripeDefaultPaymentMethodId);
            if (!protectedPaymentMethodId.Equals(profile.StripeDefaultPaymentMethodId, StringComparison.Ordinal))
            {
                profile.SetStripeDefaultPaymentMethod(protectedPaymentMethodId, profile.UpdatedAtUtc);
            }
        }
    }

    private string? DecryptNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : providerSecretProtector.Unprotect(value);
    }
}
