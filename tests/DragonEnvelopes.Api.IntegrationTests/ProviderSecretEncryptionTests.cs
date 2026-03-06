using System.Text;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using DragonEnvelopes.Infrastructure.Repositories;
using DragonEnvelopes.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DragonEnvelopes.Api.IntegrationTests;

public sealed class ProviderSecretEncryptionTests
{
    [Fact]
    public void ProviderSecretProtector_RoundTrips_AndSupportsKeyRotation()
    {
        var keyOne = Convert.ToBase64String(Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF"));
        var keyTwo = Convert.ToBase64String(Encoding.UTF8.GetBytes("ABCDEFGHIJKL0123MNOPQRST4567UVWX"));

        var oldProtector = new ProviderSecretProtector(Options.Create(new ProviderSecretEncryptionOptions
        {
            Enabled = true,
            ActiveKeyId = "k1",
            Keys = new Dictionary<string, string>
            {
                ["k1"] = keyOne
            }
        }));

        var encryptedWithOldKey = oldProtector.Protect("plaid-access-token");
        Assert.StartsWith("enc:v1:k1:", encryptedWithOldKey, StringComparison.Ordinal);

        var rotatedProtector = new ProviderSecretProtector(Options.Create(new ProviderSecretEncryptionOptions
        {
            Enabled = true,
            ActiveKeyId = "k2",
            Keys = new Dictionary<string, string>
            {
                ["k1"] = keyOne,
                ["k2"] = keyTwo
            }
        }));

        Assert.Equal("plaid-access-token", rotatedProtector.Unprotect(encryptedWithOldKey));

        var encryptedWithNewKey = rotatedProtector.Protect("stripe-customer-id");
        Assert.StartsWith("enc:v1:k2:", encryptedWithNewKey, StringComparison.Ordinal);
        Assert.Equal("stripe-customer-id", rotatedProtector.Unprotect(encryptedWithNewKey));
        Assert.Equal("legacy-plaintext", rotatedProtector.Unprotect("legacy-plaintext"));
    }

    [Fact]
    public async Task FamilyFinancialProfileRepository_EncryptsOnSave_AndDecryptsOnRead()
    {
        var key = Convert.ToBase64String(Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF"));
        var protector = BuildProtector(key);

        var dbOptions = new DbContextOptionsBuilder<DragonEnvelopesDbContext>()
            .UseInMemoryDatabase($"provider-secret-encryption-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new DragonEnvelopesDbContext(dbOptions);

        var familyId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        dbContext.Families.Add(new Family(familyId, "Security Test Family", now));
        await dbContext.SaveChangesAsync();

        var repository = new FamilyFinancialProfileRepository(dbContext, protector);
        var profile = new FamilyFinancialProfile(
            Guid.NewGuid(),
            familyId,
            plaidItemId: "item_123",
            plaidAccessToken: "access-token-plaintext",
            stripeCustomerId: "cus_123",
            stripeDefaultPaymentMethodId: "pm_456",
            createdAtUtc: now,
            updatedAtUtc: now);

        await repository.AddAsync(profile);

        var stored = await dbContext.FamilyFinancialProfiles.AsNoTracking().SingleAsync();
        Assert.True(protector.IsProtected(stored.PlaidAccessToken!));
        Assert.True(protector.IsProtected(stored.StripeCustomerId!));
        Assert.True(protector.IsProtected(stored.StripeDefaultPaymentMethodId!));

        var loaded = await repository.GetByFamilyIdAsync(familyId);
        Assert.NotNull(loaded);
        Assert.Equal("access-token-plaintext", loaded!.PlaidAccessToken);
        Assert.Equal("cus_123", loaded.StripeCustomerId);
        Assert.Equal("pm_456", loaded.StripeDefaultPaymentMethodId);
    }

    [Fact]
    public async Task FamilyFinancialProfileRepository_MigratesLegacyPlaintextToEncryptedOnSave()
    {
        var key = Convert.ToBase64String(Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF"));
        var protector = BuildProtector(key);

        var dbOptions = new DbContextOptionsBuilder<DragonEnvelopesDbContext>()
            .UseInMemoryDatabase($"provider-secret-migration-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new DragonEnvelopesDbContext(dbOptions);

        var familyId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        dbContext.Families.Add(new Family(familyId, "Migration Test Family", now));
        dbContext.FamilyFinancialProfiles.Add(new FamilyFinancialProfile(
            Guid.NewGuid(),
            familyId,
            plaidItemId: "item_legacy",
            plaidAccessToken: "legacy-access-token",
            stripeCustomerId: "legacy-customer",
            stripeDefaultPaymentMethodId: null,
            createdAtUtc: now,
            updatedAtUtc: now));
        await dbContext.SaveChangesAsync();

        var repository = new FamilyFinancialProfileRepository(dbContext, protector);
        var forUpdate = await repository.GetByFamilyIdForUpdateAsync(familyId);
        Assert.NotNull(forUpdate);
        Assert.Equal("legacy-access-token", forUpdate!.PlaidAccessToken);

        await repository.SaveChangesAsync();

        var stored = await dbContext.FamilyFinancialProfiles.AsNoTracking().SingleAsync();
        Assert.True(protector.IsProtected(stored.PlaidAccessToken!));
        Assert.True(protector.IsProtected(stored.StripeCustomerId!));
    }

    private static IProviderSecretProtector BuildProtector(string key)
    {
        return new ProviderSecretProtector(Options.Create(new ProviderSecretEncryptionOptions
        {
            Enabled = true,
            ActiveKeyId = "k1",
            Keys = new Dictionary<string, string>
            {
                ["k1"] = key
            }
        }));
    }
}
