using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;

namespace DragonEnvelopes.ProviderClients.Providers;

public sealed class StripeGateway(
    HttpClient httpClient,
    IOptions<StripeGatewayOptions> options,
    ILogger<StripeGateway> logger) : IStripeGateway
{
    public async Task<string> CreateCustomerAsync(
        Guid familyId,
        string email,
        string? name,
        CancellationToken cancellationToken = default)
    {
        var configured = options.Value;
        EnsureConfigured(configured);

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainValidationException("Customer email is required.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(configured.ApiBaseUrl, "/v1/customers"));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", configured.SecretKey);
        request.Content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("email", email.Trim()),
            new KeyValuePair<string, string>("name", string.IsNullOrWhiteSpace(name) ? email.Trim() : name.Trim()),
            new KeyValuePair<string, string>("metadata[family_id]", familyId.ToString())
        ]);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Stripe customer creation failed. Status={StatusCode}, Body={Body}",
                (int)response.StatusCode,
                payload);
            throw new DomainValidationException("Stripe customer creation failed.");
        }

        using var doc = JsonDocument.Parse(payload);
        var customerId = doc.RootElement.TryGetProperty("id", out var customerIdElement)
            ? customerIdElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new DomainValidationException("Stripe customer response was invalid.");
        }

        return customerId;
    }

    public async Task<(string SetupIntentId, string ClientSecret)> CreateSetupIntentAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        var configured = options.Value;
        EnsureConfigured(configured);

        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new DomainValidationException("Stripe customer id is required.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(configured.ApiBaseUrl, "/v1/setup_intents"));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", configured.SecretKey);
        request.Content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("customer", customerId.Trim()),
            new KeyValuePair<string, string>("usage", "off_session"),
            new KeyValuePair<string, string>("payment_method_types[]", "card"),
            new KeyValuePair<string, string>("payment_method_types[]", "us_bank_account")
        ]);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Stripe setup intent creation failed. Status={StatusCode}, Body={Body}",
                (int)response.StatusCode,
                payload);
            throw new DomainValidationException("Stripe setup intent creation failed.");
        }

        using var doc = JsonDocument.Parse(payload);
        var setupIntentId = doc.RootElement.TryGetProperty("id", out var setupIntentIdElement)
            ? setupIntentIdElement.GetString()
            : null;
        var clientSecret = doc.RootElement.TryGetProperty("client_secret", out var clientSecretElement)
            ? clientSecretElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(setupIntentId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new DomainValidationException("Stripe setup intent response was invalid.");
        }

        return (setupIntentId, clientSecret);
    }

    public async Task<string> CreateFinancialAccountAsync(
        string customerId,
        Guid familyId,
        Guid envelopeId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var configured = options.Value;
        EnsureConfigured(configured);

        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new DomainValidationException("Stripe customer id is required.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(configured.ApiBaseUrl, "/v1/treasury/financial_accounts"));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", configured.SecretKey);
        request.Content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("supported_currencies[]", "usd"),
            new KeyValuePair<string, string>("features[card_issuing][requested]", "true"),
            new KeyValuePair<string, string>("metadata[customer_id]", customerId.Trim()),
            new KeyValuePair<string, string>("metadata[family_id]", familyId.ToString()),
            new KeyValuePair<string, string>("metadata[envelope_id]", envelopeId.ToString()),
            new KeyValuePair<string, string>("metadata[display_name]", string.IsNullOrWhiteSpace(displayName) ? envelopeId.ToString() : displayName.Trim())
        ]);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Stripe financial account creation failed. Status={StatusCode}, Body={Body}",
                (int)response.StatusCode,
                payload);
            throw new DomainValidationException("Stripe financial account creation failed.");
        }

        using var doc = JsonDocument.Parse(payload);
        var accountId = doc.RootElement.TryGetProperty("id", out var accountIdElement)
            ? accountIdElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new DomainValidationException("Stripe financial account response was invalid.");
        }

        return accountId;
    }

    public async Task<(string ProviderCardId, string Status, string? Brand, string? Last4)> CreateVirtualCardAsync(
        string financialAccountId,
        Guid familyId,
        Guid envelopeId,
        string cardholderName,
        CancellationToken cancellationToken = default)
    {
        var configured = options.Value;
        EnsureConfigured(configured);

        if (string.IsNullOrWhiteSpace(financialAccountId))
        {
            throw new DomainValidationException("Stripe financial account id is required.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(configured.ApiBaseUrl, "/v1/issuing/cards"));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", configured.SecretKey);
        request.Content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("type", "virtual"),
            new KeyValuePair<string, string>("currency", "usd"),
            new KeyValuePair<string, string>("metadata[family_id]", familyId.ToString()),
            new KeyValuePair<string, string>("metadata[envelope_id]", envelopeId.ToString()),
            new KeyValuePair<string, string>("metadata[financial_account_id]", financialAccountId.Trim()),
            new KeyValuePair<string, string>("metadata[cardholder_name]", string.IsNullOrWhiteSpace(cardholderName) ? envelopeId.ToString() : cardholderName.Trim())
        ]);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Stripe virtual card issuance failed. Status={StatusCode}, Body={Body}",
                (int)response.StatusCode,
                payload);
            throw new DomainValidationException("Stripe virtual card issuance failed.");
        }

        using var doc = JsonDocument.Parse(payload);
        var cardId = doc.RootElement.TryGetProperty("id", out var cardIdElement)
            ? cardIdElement.GetString()
            : null;
        var status = doc.RootElement.TryGetProperty("status", out var statusElement)
            ? statusElement.GetString()
            : "active";
        string? brand = null;
        string? last4 = null;

        if (doc.RootElement.TryGetProperty("last4", out var last4Element))
        {
            last4 = last4Element.GetString();
        }

        if (doc.RootElement.TryGetProperty("brand", out var brandElement))
        {
            brand = brandElement.GetString();
        }

        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new DomainValidationException("Stripe virtual card response was invalid.");
        }

        return (
            cardId,
            string.IsNullOrWhiteSpace(status) ? "active" : status,
            brand,
            last4);
    }

    public async Task UpdateCardStatusAsync(
        string providerCardId,
        string status,
        CancellationToken cancellationToken = default)
    {
        var configured = options.Value;
        EnsureConfigured(configured);

        if (string.IsNullOrWhiteSpace(providerCardId))
        {
            throw new DomainValidationException("Provider card id is required.");
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new DomainValidationException("Card status is required.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            BuildUri(configured.ApiBaseUrl, $"/v1/issuing/cards/{Uri.EscapeDataString(providerCardId.Trim())}"));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", configured.SecretKey);
        request.Content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("status", status.Trim().ToLowerInvariant())
        ]);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Stripe card status update failed. Status={StatusCode}, Body={Body}",
                (int)response.StatusCode,
                payload);
            throw new DomainValidationException("Stripe card status update failed.");
        }
    }

    public async Task UpdateCardSpendingControlsAsync(
        string providerCardId,
        decimal? dailyLimitAmount,
        IReadOnlyList<string> allowedMerchantCategories,
        IReadOnlyList<string> allowedMerchantNames,
        CancellationToken cancellationToken = default)
    {
        var configured = options.Value;
        EnsureConfigured(configured);

        if (string.IsNullOrWhiteSpace(providerCardId))
        {
            throw new DomainValidationException("Provider card id is required.");
        }

        if (dailyLimitAmount.HasValue && dailyLimitAmount.Value < 0m)
        {
            throw new DomainValidationException("Daily limit amount cannot be negative.");
        }

        var formValues = new List<KeyValuePair<string, string>>();

        if (dailyLimitAmount.HasValue)
        {
            var amountInMinorUnits = decimal.Round(dailyLimitAmount.Value * 100m, 0, MidpointRounding.AwayFromZero);
            formValues.Add(new KeyValuePair<string, string>(
                "spending_controls[spending_limits][0][amount]",
                Convert.ToInt64(amountInMinorUnits, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)));
            formValues.Add(new KeyValuePair<string, string>(
                "spending_controls[spending_limits][0][interval]",
                "daily"));
        }

        for (var i = 0; i < allowedMerchantCategories.Count; i++)
        {
            formValues.Add(new KeyValuePair<string, string>(
                "spending_controls[allowed_categories][]",
                allowedMerchantCategories[i]));
        }

        var merchantNamesJson = JsonSerializer.Serialize(allowedMerchantNames);
        formValues.Add(new KeyValuePair<string, string>(
            "metadata[allowed_merchant_names]",
            merchantNamesJson));

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            BuildUri(configured.ApiBaseUrl, $"/v1/issuing/cards/{Uri.EscapeDataString(providerCardId.Trim())}"));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", configured.SecretKey);
        request.Content = new FormUrlEncodedContent(formValues);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Stripe card spending control update failed. Status={StatusCode}, Body={Body}",
                (int)response.StatusCode,
                payload);
            throw new DomainValidationException("Stripe card spending controls update failed.");
        }
    }

    private static void EnsureConfigured(StripeGatewayOptions options)
    {
        if (!options.Enabled)
        {
            throw new DomainValidationException("Stripe integration is disabled.");
        }

        if (string.IsNullOrWhiteSpace(options.SecretKey))
        {
            throw new DomainValidationException("Stripe integration is not fully configured.");
        }
    }

    private static Uri BuildUri(string baseUrl, string relative)
    {
        var normalizedBase = string.IsNullOrWhiteSpace(baseUrl)
            ? "https://api.stripe.com"
            : baseUrl.TrimEnd('/');
        return new Uri($"{normalizedBase}{relative}");
    }
}
