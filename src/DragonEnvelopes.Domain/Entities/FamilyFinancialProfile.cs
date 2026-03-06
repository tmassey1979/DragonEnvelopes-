namespace DragonEnvelopes.Domain.Entities;

public sealed class FamilyFinancialProfile
{
    public Guid Id { get; }
    public Guid FamilyId { get; }
    public string? PlaidItemId { get; private set; }
    public string? PlaidAccessToken { get; private set; }
    public string? StripeCustomerId { get; private set; }
    public string? StripeDefaultPaymentMethodId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public bool PlaidConnected => !string.IsNullOrWhiteSpace(PlaidItemId) && !string.IsNullOrWhiteSpace(PlaidAccessToken);
    public bool StripeConnected => !string.IsNullOrWhiteSpace(StripeCustomerId);

    public FamilyFinancialProfile(
        Guid id,
        Guid familyId,
        string? plaidItemId,
        string? plaidAccessToken,
        string? stripeCustomerId,
        string? stripeDefaultPaymentMethodId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Financial profile id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (updatedAtUtc < createdAtUtc)
        {
            throw new DomainValidationException("Updated time cannot be before created time.");
        }

        Id = id;
        FamilyId = familyId;
        PlaidItemId = NormalizeNullable(plaidItemId);
        PlaidAccessToken = NormalizeNullable(plaidAccessToken);
        StripeCustomerId = NormalizeNullable(stripeCustomerId);
        StripeDefaultPaymentMethodId = NormalizeNullable(stripeDefaultPaymentMethodId);
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void SetPlaidConnection(string itemId, string accessToken, DateTimeOffset updatedAtUtc)
    {
        PlaidItemId = NormalizeRequired(itemId, "Plaid item id");
        PlaidAccessToken = NormalizeRequired(accessToken, "Plaid access token");
        Touch(updatedAtUtc);
    }

    public void SetStripeCustomer(string stripeCustomerId, DateTimeOffset updatedAtUtc)
    {
        StripeCustomerId = NormalizeRequired(stripeCustomerId, "Stripe customer id");
        Touch(updatedAtUtc);
    }

    public void SetStripeDefaultPaymentMethod(string paymentMethodId, DateTimeOffset updatedAtUtc)
    {
        StripeDefaultPaymentMethodId = NormalizeRequired(paymentMethodId, "Stripe payment method id");
        Touch(updatedAtUtc);
    }

    private void Touch(DateTimeOffset updatedAtUtc)
    {
        if (updatedAtUtc < CreatedAtUtc)
        {
            throw new DomainValidationException("Updated time cannot be before created time.");
        }

        UpdatedAtUtc = updatedAtUtc;
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
