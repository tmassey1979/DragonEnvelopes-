namespace DragonEnvelopes.Contracts.Financial;

public sealed record CreatePlaidLinkTokenRequest(
    string? ClientUserId,
    string? ClientName);

public sealed record CreatePlaidLinkTokenResponse(
    string LinkToken,
    DateTimeOffset ExpiresAtUtc);

public sealed record ExchangePlaidPublicTokenRequest(
    string PublicToken);

public sealed record CreateStripeSetupIntentRequest(
    string Email,
    string? Name);

public sealed record CreateStripeSetupIntentResponse(
    string CustomerId,
    string SetupIntentId,
    string ClientSecret);

public sealed record FamilyFinancialStatusResponse(
    Guid FamilyId,
    bool PlaidConnected,
    string? PlaidItemId,
    bool StripeConnected,
    string? StripeCustomerId,
    DateTimeOffset? UpdatedAtUtc,
    decimal ReconciliationDriftThreshold = 25m);

public sealed record UpdateReconciliationDriftThresholdRequest(
    decimal ReconciliationDriftThreshold);

public sealed record RewrapProviderSecretsResponse(
    Guid FamilyId,
    bool ProfileFound,
    int FieldsTouched,
    DateTimeOffset ExecutedAtUtc);

public sealed record StripeWebhookActivityResponse(
    string ProcessingStatus,
    string EventType,
    DateTimeOffset ProcessedAtUtc,
    string? ErrorMessage);

public sealed record SpendNotificationDispatchStatusResponse(
    string Status,
    int QueuedCount,
    int SentCount,
    int FailedCount,
    DateTimeOffset? LastAttemptAtUtc,
    string? LastErrorMessage);

public sealed record ProviderActivityHealthResponse(
    Guid FamilyId,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset? LastPlaidTransactionSyncAtUtc,
    DateTimeOffset? LastPlaidBalanceRefreshAtUtc,
    int DriftedAccountCount,
    decimal TotalAbsoluteDrift,
    StripeWebhookActivityResponse? LastStripeWebhook,
    SpendNotificationDispatchStatusResponse NotificationDispatch,
    string TraceId);

public sealed record ProviderTimelineEventResponse(
    string Source,
    string EventType,
    string Status,
    DateTimeOffset OccurredAtUtc,
    string Summary,
    string? Detail,
    Guid? StripeWebhookEventId,
    Guid? PlaidWebhookEventId,
    Guid? NotificationDispatchEventId,
    Guid? ReconciliationAlertEventId);

public sealed record ProviderActivityTimelineResponse(
    Guid FamilyId,
    DateTimeOffset GeneratedAtUtc,
    int RequestedTake,
    IReadOnlyList<ProviderTimelineEventResponse> Events,
    string TraceId);

public sealed record ProviderTimelineEventDetailResponse(
    Guid FamilyId,
    string Source,
    Guid EventId,
    string EventType,
    string Status,
    DateTimeOffset OccurredAtUtc,
    string Summary,
    string? Detail,
    string? PayloadPreviewJson,
    bool PayloadTruncated);

public sealed record CreateStripeEnvelopeFinancialAccountRequest(
    string? DisplayName);

public sealed record EnvelopeFinancialAccountResponse(
    Guid Id,
    Guid FamilyId,
    Guid EnvelopeId,
    string Provider,
    string ProviderFinancialAccountId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateVirtualEnvelopeCardRequest(
    string? CardholderName);

public sealed record CreatePhysicalEnvelopeCardRequest(
    string? CardholderName,
    string RecipientName,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string StateOrProvince,
    string PostalCode,
    string CountryCode);

public sealed record EnvelopePaymentCardResponse(
    Guid Id,
    Guid FamilyId,
    Guid EnvelopeId,
    Guid EnvelopeFinancialAccountId,
    string Provider,
    string ProviderCardId,
    string Type,
    string Status,
    string? Brand,
    string? Last4,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record EnvelopePaymentCardShipmentResponse(
    Guid Id,
    Guid FamilyId,
    Guid EnvelopeId,
    Guid CardId,
    string RecipientName,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string StateOrProvince,
    string PostalCode,
    string CountryCode,
    string Status,
    string? Carrier,
    string? TrackingNumber,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record EnvelopePhysicalCardIssuanceResponse(
    EnvelopePaymentCardResponse Card,
    EnvelopePaymentCardShipmentResponse Shipment);

public sealed record UpsertEnvelopePaymentCardControlRequest(
    decimal? DailyLimitAmount,
    IReadOnlyList<string>? AllowedMerchantCategories,
    IReadOnlyList<string>? AllowedMerchantNames);

public sealed record EnvelopePaymentCardControlResponse(
    Guid Id,
    Guid FamilyId,
    Guid EnvelopeId,
    Guid CardId,
    decimal? DailyLimitAmount,
    IReadOnlyList<string> AllowedMerchantCategories,
    IReadOnlyList<string> AllowedMerchantNames,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record EnvelopePaymentCardControlAuditResponse(
    Guid Id,
    Guid FamilyId,
    Guid EnvelopeId,
    Guid CardId,
    string Action,
    string? PreviousStateJson,
    string NewStateJson,
    string ChangedBy,
    DateTimeOffset ChangedAtUtc);

public sealed record EvaluateEnvelopeCardSpendRequest(
    string MerchantName,
    string? MerchantCategory,
    decimal Amount,
    decimal SpentTodayAmount);

public sealed record EvaluateEnvelopeCardSpendResponse(
    bool IsAllowed,
    string? DenialReason,
    decimal? RemainingDailyLimit);

public sealed record StripeWebhookProcessResponse(
    string Outcome,
    string? EventId,
    string? EventType,
    string? Message);

public sealed record PlaidWebhookProcessResponse(
    string Outcome,
    string? WebhookType,
    string? WebhookCode,
    string? ItemId,
    Guid? FamilyId,
    string? Message);

public sealed record UpdateNotificationPreferenceRequest(
    bool EmailEnabled,
    bool InAppEnabled,
    bool SmsEnabled);

public sealed record NotificationPreferenceResponse(
    Guid FamilyId,
    string UserId,
    bool EmailEnabled,
    bool InAppEnabled,
    bool SmsEnabled,
    DateTimeOffset UpdatedAtUtc);

public sealed record FailedNotificationDispatchEventResponse(
    Guid Id,
    Guid FamilyId,
    string UserId,
    Guid EnvelopeId,
    Guid CardId,
    string Channel,
    decimal Amount,
    string Merchant,
    string Status,
    int AttemptCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastAttemptAtUtc,
    string? ErrorMessage);

public sealed record RetryNotificationDispatchEventResponse(
    Guid Id,
    Guid FamilyId,
    string Status,
    int AttemptCount,
    DateTimeOffset? LastAttemptAtUtc,
    DateTimeOffset? SentAtUtc,
    string? ErrorMessage);

public sealed record ReplayStripeWebhookEventResponse(
    Guid Id,
    Guid? FamilyId,
    string Status,
    string Outcome,
    DateTimeOffset ProcessedAtUtc,
    string? ErrorMessage);

public sealed record CreatePlaidAccountLinkRequest(
    Guid AccountId,
    string PlaidAccountId);

public sealed record PlaidAccountLinkResponse(
    Guid Id,
    Guid FamilyId,
    Guid AccountId,
    string PlaidAccountId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PlaidTransactionSyncResponse(
    Guid FamilyId,
    int PulledCount,
    int InsertedCount,
    int DedupedCount,
    int UnmappedCount,
    string? NextCursor,
    DateTimeOffset ProcessedAtUtc);

public sealed record PlaidBalanceRefreshResponse(
    Guid FamilyId,
    int RefreshedCount,
    int DriftedCount,
    decimal TotalAbsoluteDrift,
    DateTimeOffset RefreshedAtUtc);

public sealed record PlaidReconciliationAccountResponse(
    Guid AccountId,
    string AccountName,
    string PlaidAccountId,
    decimal InternalBalance,
    decimal ProviderBalance,
    decimal DriftAmount,
    bool IsDrifted);

public sealed record PlaidReconciliationReportResponse(
    Guid FamilyId,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<PlaidReconciliationAccountResponse> Accounts);
