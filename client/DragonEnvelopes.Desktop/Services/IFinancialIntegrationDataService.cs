using DragonEnvelopes.Contracts.Financial;

namespace DragonEnvelopes.Desktop.Services;

public interface IFinancialIntegrationDataService
{
    Task<FamilyFinancialStatusResponse> GetFamilyFinancialStatusAsync(CancellationToken cancellationToken = default);

    Task<ProviderActivityHealthResponse> GetProviderActivityHealthAsync(CancellationToken cancellationToken = default);

    Task<ProviderActivityTimelineResponse> GetProviderActivityTimelineAsync(
        int take = 25,
        CancellationToken cancellationToken = default);

    Task<NotificationPreferenceResponse> GetNotificationPreferenceAsync(CancellationToken cancellationToken = default);

    Task<NotificationPreferenceResponse> UpdateNotificationPreferenceAsync(
        bool emailEnabled,
        bool inAppEnabled,
        bool smsEnabled,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FailedNotificationDispatchEventResponse>> ListFailedNotificationDispatchEventsAsync(
        int take = 25,
        CancellationToken cancellationToken = default);

    Task<RetryNotificationDispatchEventResponse> RetryFailedNotificationDispatchEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<CreatePlaidLinkTokenResponse> CreatePlaidLinkTokenAsync(
        string? clientName,
        CancellationToken cancellationToken = default);

    Task<FamilyFinancialStatusResponse> ExchangePlaidPublicTokenAsync(
        string publicToken,
        CancellationToken cancellationToken = default);

    Task<PlaidAccountLinkResponse> UpsertPlaidAccountLinkAsync(
        Guid accountId,
        string plaidAccountId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlaidAccountLinkResponse>> ListPlaidAccountLinksAsync(CancellationToken cancellationToken = default);

    Task DeletePlaidAccountLinkAsync(
        Guid linkId,
        CancellationToken cancellationToken = default);

    Task<PlaidTransactionSyncResponse> SyncPlaidTransactionsAsync(CancellationToken cancellationToken = default);

    Task<PlaidBalanceRefreshResponse> RefreshPlaidBalancesAsync(CancellationToken cancellationToken = default);

    Task<PlaidReconciliationReportResponse> GetPlaidReconciliationReportAsync(CancellationToken cancellationToken = default);

    Task<CreateStripeSetupIntentResponse> CreateStripeSetupIntentAsync(
        string email,
        string? name,
        CancellationToken cancellationToken = default);

    Task<EnvelopeFinancialAccountResponse> LinkStripeEnvelopeFinancialAccountAsync(
        Guid envelopeId,
        string? displayName,
        CancellationToken cancellationToken = default);

    Task<EnvelopeFinancialAccountResponse?> GetEnvelopeFinancialAccountAsync(
        Guid envelopeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnvelopeFinancialAccountResponse>> ListFamilyFinancialAccountsAsync(CancellationToken cancellationToken = default);

    Task<EnvelopePaymentCardResponse> IssueVirtualCardAsync(
        Guid envelopeId,
        string? cardholderName,
        CancellationToken cancellationToken = default);

    Task<EnvelopePhysicalCardIssuanceResponse> IssuePhysicalCardAsync(
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

    Task<IReadOnlyList<EnvelopePaymentCardResponse>> ListEnvelopeCardsAsync(
        Guid envelopeId,
        CancellationToken cancellationToken = default);

    Task<EnvelopePaymentCardResponse> FreezeCardAsync(
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default);

    Task<EnvelopePaymentCardResponse> UnfreezeCardAsync(
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default);

    Task<EnvelopePaymentCardResponse> CancelCardAsync(
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default);

    Task<EnvelopePhysicalCardIssuanceResponse?> GetPhysicalCardIssuanceAsync(
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default);

    Task<EnvelopePhysicalCardIssuanceResponse> RefreshPhysicalCardIssuanceAsync(
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default);

    Task<EnvelopePaymentCardControlResponse?> GetCardControlsAsync(
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default);

    Task<EnvelopePaymentCardControlResponse> UpsertCardControlsAsync(
        Guid envelopeId,
        Guid cardId,
        decimal? dailyLimitAmount,
        IReadOnlyList<string>? allowedMerchantCategories,
        IReadOnlyList<string>? allowedMerchantNames,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnvelopePaymentCardControlAuditResponse>> ListCardControlAuditAsync(
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default);

    Task<EvaluateEnvelopeCardSpendResponse> EvaluateCardSpendAsync(
        Guid envelopeId,
        Guid cardId,
        string merchantName,
        string? merchantCategory,
        decimal amount,
        decimal spentTodayAmount,
        CancellationToken cancellationToken = default);
}
