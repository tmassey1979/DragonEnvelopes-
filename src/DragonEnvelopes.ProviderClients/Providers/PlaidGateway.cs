using System.Text;
using System.Text.Json;
using System.Globalization;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DragonEnvelopes.ProviderClients.Providers;

public sealed class PlaidGateway(
    HttpClient httpClient,
    IOptions<PlaidGatewayOptions> options,
    ILogger<PlaidGateway> logger) : IPlaidGateway
{
    public async Task<(string LinkToken, DateTimeOffset ExpiresAtUtc)> CreateLinkTokenAsync(
        Guid familyId,
        string clientUserId,
        string clientName,
        CancellationToken cancellationToken = default)
    {
        var configured = options.Value;
        EnsureConfigured(configured);

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(configured.BaseUrl, "/link/token/create"));
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                client_id = configured.ClientId,
                secret = configured.Secret,
                client_name = string.IsNullOrWhiteSpace(clientName) ? configured.ClientName : clientName,
                language = configured.Language,
                country_codes = configured.CountryCodes,
                products = configured.Products,
                user = new
                {
                    client_user_id = string.IsNullOrWhiteSpace(clientUserId)
                        ? familyId.ToString("N")
                        : clientUserId
                }
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Plaid link token creation failed. Status={StatusCode}, Body={Body}",
                (int)response.StatusCode,
                payload);
            throw new DomainValidationException("Plaid link token creation failed.");
        }

        using var doc = JsonDocument.Parse(payload);
        var linkToken = doc.RootElement.TryGetProperty("link_token", out var linkTokenElement)
            ? linkTokenElement.GetString()
            : null;
        var expirationText = doc.RootElement.TryGetProperty("expiration", out var expirationElement)
            ? expirationElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(linkToken) || !DateTimeOffset.TryParse(expirationText, out var expiration))
        {
            throw new DomainValidationException("Plaid link token response was invalid.");
        }

        return (linkToken, expiration);
    }

    public async Task<(string ItemId, string AccessToken)> ExchangePublicTokenAsync(
        string publicToken,
        CancellationToken cancellationToken = default)
    {
        var configured = options.Value;
        EnsureConfigured(configured);

        if (string.IsNullOrWhiteSpace(publicToken))
        {
            throw new DomainValidationException("Plaid public token is required.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(configured.BaseUrl, "/item/public_token/exchange"));
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                client_id = configured.ClientId,
                secret = configured.Secret,
                public_token = publicToken.Trim()
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Plaid token exchange failed. Status={StatusCode}, Body={Body}",
                (int)response.StatusCode,
                payload);
            throw new DomainValidationException("Plaid token exchange failed.");
        }

        using var doc = JsonDocument.Parse(payload);
        var accessToken = doc.RootElement.TryGetProperty("access_token", out var accessTokenElement)
            ? accessTokenElement.GetString()
            : null;
        var itemId = doc.RootElement.TryGetProperty("item_id", out var itemIdElement)
            ? itemIdElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(itemId) || string.IsNullOrWhiteSpace(accessToken))
        {
            throw new DomainValidationException("Plaid token exchange response was invalid.");
        }

        return (itemId, accessToken);
    }

    public async Task<PlaidTransactionSyncResult> SyncTransactionsAsync(
        string accessToken,
        string? cursor,
        int count,
        CancellationToken cancellationToken = default)
    {
        var configured = options.Value;
        EnsureConfigured(configured);

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new DomainValidationException("Plaid access token is required.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(configured.BaseUrl, "/transactions/sync"));
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                client_id = configured.ClientId,
                secret = configured.Secret,
                access_token = accessToken.Trim(),
                cursor = string.IsNullOrWhiteSpace(cursor) ? null : cursor.Trim(),
                count = count <= 0 ? 100 : count
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Plaid transaction sync failed. Status={StatusCode}, Body={Body}",
                (int)response.StatusCode,
                payload);
            throw new DomainValidationException("Plaid transaction sync failed.");
        }

        using var doc = JsonDocument.Parse(payload);
        var nextCursor = doc.RootElement.TryGetProperty("next_cursor", out var nextCursorElement)
            ? nextCursorElement.GetString()
            : null;
        var hasMore = doc.RootElement.TryGetProperty("has_more", out var hasMoreElement)
                      && hasMoreElement.ValueKind == JsonValueKind.True;

        var added = ParseTransactions(doc.RootElement, "added");
        var modified = ParseTransactions(doc.RootElement, "modified");
        return new PlaidTransactionSyncResult(
            nextCursor,
            hasMore,
            added,
            modified);
    }

    private static void EnsureConfigured(PlaidGatewayOptions options)
    {
        if (!options.Enabled)
        {
            throw new DomainValidationException("Plaid integration is disabled.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientId) || string.IsNullOrWhiteSpace(options.Secret))
        {
            throw new DomainValidationException("Plaid integration is not fully configured.");
        }
    }

    private static Uri BuildUri(string baseUrl, string relative)
    {
        var normalizedBase = string.IsNullOrWhiteSpace(baseUrl)
            ? "https://sandbox.plaid.com"
            : baseUrl.TrimEnd('/');
        return new Uri($"{normalizedBase}{relative}");
    }

    private static IReadOnlyList<PlaidTransactionRecord> ParseTransactions(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var items) || items.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var parsed = new List<PlaidTransactionRecord>();
        foreach (var item in items.EnumerateArray())
        {
            var plaidTransactionId = item.TryGetProperty("transaction_id", out var transactionIdElement)
                ? transactionIdElement.GetString()
                : null;
            var plaidAccountId = item.TryGetProperty("account_id", out var accountIdElement)
                ? accountIdElement.GetString()
                : null;
            var amount = item.TryGetProperty("amount", out var amountElement) && amountElement.TryGetDecimal(out var parsedAmount)
                ? parsedAmount
                : 0m;
            var description = item.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString()
                : null;
            var merchant = item.TryGetProperty("merchant_name", out var merchantElement)
                ? merchantElement.GetString()
                : null;

            var occurredAtUtc = DateTimeOffset.UtcNow;
            if (item.TryGetProperty("datetime", out var datetimeElement)
                && DateTimeOffset.TryParse(datetimeElement.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsedDateTime))
            {
                occurredAtUtc = parsedDateTime.ToUniversalTime();
            }
            else if (item.TryGetProperty("authorized_datetime", out var authDateTimeElement)
                     && DateTimeOffset.TryParse(authDateTimeElement.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsedAuthDateTime))
            {
                occurredAtUtc = parsedAuthDateTime.ToUniversalTime();
            }
            else if (item.TryGetProperty("date", out var dateElement)
                     && DateOnly.TryParse(dateElement.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDateOnly))
            {
                occurredAtUtc = new DateTimeOffset(parsedDateOnly.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            }

            if (string.IsNullOrWhiteSpace(plaidTransactionId) || string.IsNullOrWhiteSpace(plaidAccountId))
            {
                continue;
            }

            var normalizedDescription = string.IsNullOrWhiteSpace(description) ? "Plaid Transaction" : description.Trim();
            var normalizedMerchant = string.IsNullOrWhiteSpace(merchant) ? normalizedDescription : merchant.Trim();

            parsed.Add(new PlaidTransactionRecord(
                plaidTransactionId,
                plaidAccountId,
                amount,
                normalizedDescription,
                normalizedMerchant,
                occurredAtUtc));
        }

        return parsed;
    }
}
