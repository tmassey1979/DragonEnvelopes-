using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DragonEnvelopes.Contracts.Financial;
using DragonEnvelopes.Desktop.Api;

namespace DragonEnvelopes.Desktop.Services;

public sealed class FinancialIntegrationDataService : IFinancialIntegrationDataService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IBackendApiClient _apiClient;
    private readonly IFamilyContext _familyContext;

    public FinancialIntegrationDataService(IBackendApiClient apiClient, IFamilyContext familyContext)
    {
        _apiClient = apiClient;
        _familyContext = familyContext;
    }

    public Task<FamilyFinancialStatusResponse> GetFamilyFinancialStatusAsync(CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        return GetAsync<FamilyFinancialStatusResponse>(
            $"families/{familyId}/financial/status",
            "Loading family financial status",
            cancellationToken);
    }

    public Task<ProviderActivityHealthResponse> GetProviderActivityHealthAsync(CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        return GetAsync<ProviderActivityHealthResponse>(
            $"families/{familyId}/financial/provider-activity",
            "Loading provider activity health",
            cancellationToken);
    }

    public Task<ProviderActivityTimelineResponse> GetProviderActivityTimelineAsync(
        int take = 25,
        string? sourceFilter = null,
        string? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var normalizedTake = take <= 0 ? 25 : take;
        var queryParameters = new List<string> { $"take={normalizedTake}" };

        if (!string.IsNullOrWhiteSpace(sourceFilter))
        {
            queryParameters.Add($"source={Uri.EscapeDataString(sourceFilter.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            queryParameters.Add($"status={Uri.EscapeDataString(statusFilter.Trim())}");
        }

        return GetAsync<ProviderActivityTimelineResponse>(
            $"families/{familyId}/financial/provider-activity/timeline?{string.Join("&", queryParameters)}",
            "Loading provider activity timeline",
            cancellationToken);
    }

    public Task<NotificationPreferenceResponse> GetNotificationPreferenceAsync(CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        return GetAsync<NotificationPreferenceResponse>(
            $"families/{familyId}/notifications/preferences",
            "Loading notification preferences",
            cancellationToken);
    }

    public Task<NotificationPreferenceResponse> UpdateNotificationPreferenceAsync(
        bool emailEnabled,
        bool inAppEnabled,
        bool smsEnabled,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new UpdateNotificationPreferenceRequest(emailEnabled, inAppEnabled, smsEnabled);
        return SendAsync<NotificationPreferenceResponse>(
            HttpMethod.Put,
            $"families/{familyId}/notifications/preferences",
            payload,
            "Saving notification preferences",
            cancellationToken);
    }

    public Task<IReadOnlyList<FailedNotificationDispatchEventResponse>> ListFailedNotificationDispatchEventsAsync(
        int take = 25,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var normalizedTake = take <= 0 ? 25 : take;
        return GetListAsync<FailedNotificationDispatchEventResponse>(
            $"families/{familyId}/notifications/dispatch-events/failed?take={normalizedTake}",
            "Loading failed notification dispatch events",
            cancellationToken);
    }

    public Task<RetryNotificationDispatchEventResponse> RetryFailedNotificationDispatchEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        return SendAsync<RetryNotificationDispatchEventResponse>(
            HttpMethod.Post,
            $"families/{familyId}/notifications/dispatch-events/{eventId}/retry",
            payload: null,
            "Retrying failed notification dispatch event",
            cancellationToken);
    }

    public Task<RetryNotificationDispatchEventResponse> ReplayTimelineNotificationDispatchEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        return SendAsync<RetryNotificationDispatchEventResponse>(
            HttpMethod.Post,
            $"families/{familyId}/financial/provider-activity/timeline/notifications/{eventId}/replay",
            payload: null,
            "Replaying timeline notification dispatch event",
            cancellationToken);
    }

    public Task<ReplayStripeWebhookEventResponse> ReplayTimelineStripeWebhookEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        return SendAsync<ReplayStripeWebhookEventResponse>(
            HttpMethod.Post,
            $"families/{familyId}/financial/provider-activity/timeline/stripe-webhooks/{eventId}/replay",
            payload: null,
            "Replaying timeline Stripe webhook event",
            cancellationToken);
    }

    public async Task<StripeWebhookProcessResponse> ProcessStripeWebhookAsync(
        string payload,
        string? stripeSignature,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "webhooks/stripe")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(stripeSignature))
        {
            request.Headers.TryAddWithoutValidation("Stripe-Signature", stripeSignature.Trim());
        }

        using var response = await _apiClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "Processing Stripe webhook", cancellationToken);
        return await DeserializeAsync<StripeWebhookProcessResponse>(response, "Processing Stripe webhook", cancellationToken);
    }

    public async Task<PlaidWebhookProcessResponse> ProcessPlaidWebhookAsync(
        string payload,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "webhooks/plaid")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        using var response = await _apiClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "Processing Plaid webhook", cancellationToken);
        return await DeserializeAsync<PlaidWebhookProcessResponse>(response, "Processing Plaid webhook", cancellationToken);
    }

    public Task<RewrapProviderSecretsResponse> RewrapProviderSecretsAsync(CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        return SendAsync<RewrapProviderSecretsResponse>(
            HttpMethod.Post,
            $"families/{familyId}/financial/security/rewrap-provider-secrets",
            payload: null,
            "Rewrapping provider secrets",
            cancellationToken);
    }

    public Task<CreatePlaidLinkTokenResponse> CreatePlaidLinkTokenAsync(
        string? clientName,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new CreatePlaidLinkTokenRequest(ClientUserId: null, ClientName: clientName);
        return SendAsync<CreatePlaidLinkTokenResponse>(
            HttpMethod.Post,
            $"families/{familyId}/financial/plaid/link-token",
            payload,
            "Creating Plaid link token",
            cancellationToken);
    }

    public Task<FamilyFinancialStatusResponse> ExchangePlaidPublicTokenAsync(
        string publicToken,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new ExchangePlaidPublicTokenRequest(publicToken);
        return SendAsync<FamilyFinancialStatusResponse>(
            HttpMethod.Post,
            $"families/{familyId}/financial/plaid/exchange-public-token",
            payload,
            "Exchanging Plaid public token",
            cancellationToken);
    }

    public Task<PlaidAccountLinkResponse> UpsertPlaidAccountLinkAsync(
        Guid accountId,
        string plaidAccountId,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new CreatePlaidAccountLinkRequest(accountId, plaidAccountId);
        return SendAsync<PlaidAccountLinkResponse>(
            HttpMethod.Post,
            $"families/{familyId}/financial/plaid/account-links",
            payload,
            "Linking Plaid account",
            cancellationToken);
    }

    public Task<IReadOnlyList<PlaidAccountLinkResponse>> ListPlaidAccountLinksAsync(CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        return GetListAsync<PlaidAccountLinkResponse>(
            $"families/{familyId}/financial/plaid/account-links",
            "Loading Plaid account links",
            cancellationToken);
    }

    public async Task DeletePlaidAccountLinkAsync(
        Guid linkId,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"families/{familyId}/financial/plaid/account-links/{linkId}");
        using var response = await _apiClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, "Deleting Plaid account link", cancellationToken);
    }

    public Task<PlaidTransactionSyncResponse> SyncPlaidTransactionsAsync(CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        return SendAsync<PlaidTransactionSyncResponse>(
            HttpMethod.Post,
            $"families/{familyId}/financial/plaid/sync-transactions",
            payload: null,
            "Syncing Plaid transactions",
            cancellationToken);
    }

    public Task<PlaidBalanceRefreshResponse> RefreshPlaidBalancesAsync(CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        return SendAsync<PlaidBalanceRefreshResponse>(
            HttpMethod.Post,
            $"families/{familyId}/financial/plaid/refresh-balances",
            payload: null,
            "Refreshing Plaid balances",
            cancellationToken);
    }

    public Task<PlaidReconciliationReportResponse> GetPlaidReconciliationReportAsync(CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        return GetAsync<PlaidReconciliationReportResponse>(
            $"families/{familyId}/financial/plaid/reconciliation",
            "Loading Plaid reconciliation report",
            cancellationToken);
    }

    public Task<CreateStripeSetupIntentResponse> CreateStripeSetupIntentAsync(
        string email,
        string? name,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new CreateStripeSetupIntentRequest(email, name);
        return SendAsync<CreateStripeSetupIntentResponse>(
            HttpMethod.Post,
            $"families/{familyId}/financial/stripe/setup-intent",
            payload,
            "Creating Stripe setup intent",
            cancellationToken);
    }

    public Task<EnvelopeFinancialAccountResponse> LinkStripeEnvelopeFinancialAccountAsync(
        Guid envelopeId,
        string? displayName,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new CreateStripeEnvelopeFinancialAccountRequest(displayName);
        return SendAsync<EnvelopeFinancialAccountResponse>(
            HttpMethod.Post,
            $"families/{familyId}/envelopes/{envelopeId}/financial-accounts/stripe",
            payload,
            "Linking Stripe financial account",
            cancellationToken);
    }

    public async Task<EnvelopeFinancialAccountResponse?> GetEnvelopeFinancialAccountAsync(
        Guid envelopeId,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        using var response = await _apiClient.GetAsync(
            $"families/{familyId}/envelopes/{envelopeId}/financial-account",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, "Loading envelope financial account", cancellationToken);
        return await DeserializeAsync<EnvelopeFinancialAccountResponse>(response, "Loading envelope financial account", cancellationToken);
    }

    public Task<IReadOnlyList<EnvelopeFinancialAccountResponse>> ListFamilyFinancialAccountsAsync(CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        return GetListAsync<EnvelopeFinancialAccountResponse>(
            $"families/{familyId}/financial-accounts",
            "Loading family financial accounts",
            cancellationToken);
    }

    public Task<EnvelopePaymentCardResponse> IssueVirtualCardAsync(
        Guid envelopeId,
        string? cardholderName,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new CreateVirtualEnvelopeCardRequest(cardholderName);
        return SendAsync<EnvelopePaymentCardResponse>(
            HttpMethod.Post,
            $"families/{familyId}/envelopes/{envelopeId}/cards/virtual",
            payload,
            "Issuing virtual card",
            cancellationToken);
    }

    public Task<EnvelopePhysicalCardIssuanceResponse> IssuePhysicalCardAsync(
        Guid envelopeId,
        string? cardholderName,
        string recipientName,
        string addressLine1,
        string? addressLine2,
        string city,
        string stateOrProvince,
        string postalCode,
        string countryCode,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new CreatePhysicalEnvelopeCardRequest(
            cardholderName,
            recipientName,
            addressLine1,
            addressLine2,
            city,
            stateOrProvince,
            postalCode,
            countryCode);

        return SendAsync<EnvelopePhysicalCardIssuanceResponse>(
            HttpMethod.Post,
            $"families/{familyId}/envelopes/{envelopeId}/cards/physical",
            payload,
            "Issuing physical card",
            cancellationToken);
    }

    public Task<IReadOnlyList<EnvelopePaymentCardResponse>> ListEnvelopeCardsAsync(
        Guid envelopeId,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        return GetListAsync<EnvelopePaymentCardResponse>(
            $"families/{familyId}/envelopes/{envelopeId}/cards",
            "Loading envelope cards",
            cancellationToken);
    }

    public Task<EnvelopePaymentCardResponse> FreezeCardAsync(
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        return UpdateCardStateAsync(envelopeId, cardId, "freeze", "Freezing card", cancellationToken);
    }

    public Task<EnvelopePaymentCardResponse> UnfreezeCardAsync(
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        return UpdateCardStateAsync(envelopeId, cardId, "unfreeze", "Unfreezing card", cancellationToken);
    }

    public Task<EnvelopePaymentCardResponse> CancelCardAsync(
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        return UpdateCardStateAsync(envelopeId, cardId, "cancel", "Canceling card", cancellationToken);
    }

    public async Task<EnvelopePhysicalCardIssuanceResponse?> GetPhysicalCardIssuanceAsync(
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        using var response = await _apiClient.GetAsync(
            $"families/{familyId}/envelopes/{envelopeId}/cards/{cardId}/issuance",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, "Loading physical card issuance status", cancellationToken);
        return await DeserializeAsync<EnvelopePhysicalCardIssuanceResponse>(response, "Loading physical card issuance status", cancellationToken);
    }

    public Task<EnvelopePhysicalCardIssuanceResponse> RefreshPhysicalCardIssuanceAsync(
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        return SendAsync<EnvelopePhysicalCardIssuanceResponse>(
            HttpMethod.Post,
            $"families/{familyId}/envelopes/{envelopeId}/cards/{cardId}/issuance/refresh",
            payload: null,
            "Refreshing physical card issuance status",
            cancellationToken);
    }

    public async Task<EnvelopePaymentCardControlResponse?> GetCardControlsAsync(
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        using var response = await _apiClient.GetAsync(
            $"families/{familyId}/envelopes/{envelopeId}/cards/{cardId}/controls",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, "Loading card controls", cancellationToken);
        return await DeserializeAsync<EnvelopePaymentCardControlResponse>(response, "Loading card controls", cancellationToken);
    }

    public Task<EnvelopePaymentCardControlResponse> UpsertCardControlsAsync(
        Guid envelopeId,
        Guid cardId,
        decimal? dailyLimitAmount,
        IReadOnlyList<string>? allowedMerchantCategories,
        IReadOnlyList<string>? allowedMerchantNames,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new UpsertEnvelopePaymentCardControlRequest(
            dailyLimitAmount,
            allowedMerchantCategories,
            allowedMerchantNames);
        return SendAsync<EnvelopePaymentCardControlResponse>(
            HttpMethod.Put,
            $"families/{familyId}/envelopes/{envelopeId}/cards/{cardId}/controls",
            payload,
            "Saving card controls",
            cancellationToken);
    }

    public Task<IReadOnlyList<EnvelopePaymentCardControlAuditResponse>> ListCardControlAuditAsync(
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        return GetListAsync<EnvelopePaymentCardControlAuditResponse>(
            $"families/{familyId}/envelopes/{envelopeId}/cards/{cardId}/controls/audit",
            "Loading card control audit history",
            cancellationToken);
    }

    public Task<EvaluateEnvelopeCardSpendResponse> EvaluateCardSpendAsync(
        Guid envelopeId,
        Guid cardId,
        string merchantName,
        string? merchantCategory,
        decimal amount,
        decimal spentTodayAmount,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new EvaluateEnvelopeCardSpendRequest(
            merchantName,
            merchantCategory,
            amount,
            spentTodayAmount);
        return SendAsync<EvaluateEnvelopeCardSpendResponse>(
            HttpMethod.Post,
            $"families/{familyId}/envelopes/{envelopeId}/cards/{cardId}/controls/evaluate",
            payload,
            "Evaluating card spend",
            cancellationToken);
    }

    private Task<EnvelopePaymentCardResponse> UpdateCardStateAsync(
        Guid envelopeId,
        Guid cardId,
        string action,
        string operation,
        CancellationToken cancellationToken)
    {
        var familyId = RequireFamilyId();
        return SendAsync<EnvelopePaymentCardResponse>(
            HttpMethod.Post,
            $"families/{familyId}/envelopes/{envelopeId}/cards/{cardId}/{action}",
            payload: null,
            operation,
            cancellationToken);
    }

    private async Task<T> GetAsync<T>(string path, string operation, CancellationToken cancellationToken)
    {
        using var response = await _apiClient.GetAsync(path, cancellationToken);
        await EnsureSuccessAsync(response, operation, cancellationToken);
        return await DeserializeAsync<T>(response, operation, cancellationToken);
    }

    private async Task<IReadOnlyList<T>> GetListAsync<T>(string path, string operation, CancellationToken cancellationToken)
    {
        using var response = await _apiClient.GetAsync(path, cancellationToken);
        await EnsureSuccessAsync(response, operation, cancellationToken);
        return await DeserializeAsync<List<T>>(response, operation, cancellationToken) ?? [];
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object? payload,
        string operation,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload, options: SerializerOptions);
        }

        using var response = await _apiClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, operation, cancellationToken);
        return await DeserializeAsync<T>(response, operation, cancellationToken);
    }

    private static async Task<T> DeserializeAsync<T>(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions, cancellationToken);
        return payload is null
            ? throw new InvalidOperationException($"{operation} returned an empty response.")
            : payload;
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var detail = await ReadErrorDetailAsync(response, cancellationToken);
        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(detail)
                ? $"{operation} failed with status {(int)response.StatusCode}."
                : $"{operation} failed with status {(int)response.StatusCode}: {detail}");
    }

    private static async Task<string> ReadErrorDetailAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (document.RootElement.TryGetProperty("detail", out var detailElement))
                {
                    return detailElement.GetString() ?? payload;
                }

                if (document.RootElement.TryGetProperty("title", out var titleElement))
                {
                    return titleElement.GetString() ?? payload;
                }
            }
        }
        catch (JsonException)
        {
            // Use raw payload fallback for non-JSON responses.
        }

        return payload;
    }

    private Guid RequireFamilyId()
    {
        if (!_familyContext.FamilyId.HasValue)
        {
            throw new InvalidOperationException("No family selected for financial integrations.");
        }

        return _familyContext.FamilyId.Value;
    }
}
