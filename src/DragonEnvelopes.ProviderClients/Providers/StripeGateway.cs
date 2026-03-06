using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
