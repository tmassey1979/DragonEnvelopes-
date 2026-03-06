using System.Text.Json;
using DragonEnvelopes.Contracts.Financial;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.Tests;

internal sealed class FakeFinancialIntegrationDataService : IFinancialIntegrationDataService
{
    private readonly Guid _familyId;
    private readonly Guid _financialAccountId;
    private readonly Dictionary<Guid, EnvelopeFinancialAccountResponse?> _financialAccountByEnvelope = new();
    private readonly Dictionary<Guid, List<EnvelopePaymentCardResponse>> _cardsByEnvelope = new();
    private readonly Dictionary<(Guid EnvelopeId, Guid CardId), EnvelopePhysicalCardIssuanceResponse?> _issuanceByCard = new();
    private readonly Dictionary<(Guid EnvelopeId, Guid CardId), EnvelopePaymentCardControlResponse?> _controlsByCard = new();
    private readonly Dictionary<(Guid EnvelopeId, Guid CardId), List<EnvelopePaymentCardControlAuditResponse>> _auditByCard = new();

    public FakeFinancialIntegrationDataService(
        Guid familyId,
        Guid accountId,
        Guid envelopeId,
        Guid financialAccountId,
        Guid cardId)
    {
        _familyId = familyId;
        _financialAccountId = financialAccountId;

        var now = DateTimeOffset.UtcNow;
        StatusResponse = new FamilyFinancialStatusResponse(
            familyId,
            PlaidConnected: true,
            PlaidItemId: "plaid_item_test_001",
            StripeConnected: true,
            StripeCustomerId: "cus_initial_001",
            UpdatedAtUtc: now);
        ExchangeStatusResponse = new FamilyFinancialStatusResponse(
            familyId,
            PlaidConnected: true,
            PlaidItemId: "plaid_item_after_exchange",
            StripeConnected: true,
            StripeCustomerId: "cus_initial_001",
            UpdatedAtUtc: now.AddMinutes(1));
        NotificationPreferenceResponse = new NotificationPreferenceResponse(
            familyId,
            "test-user",
            EmailEnabled: true,
            InAppEnabled: true,
            SmsEnabled: false,
            UpdatedAtUtc: now);
        RewrapProviderSecretsResponse = new RewrapProviderSecretsResponse(
            FamilyId: familyId,
            ProfileFound: true,
            FieldsTouched: 3,
            ExecutedAtUtc: now);
        PlaidLinkTokenResponse = new CreatePlaidLinkTokenResponse("link-token-001", now.AddMinutes(30));
        PlaidSyncResponse = new PlaidTransactionSyncResponse(
            familyId,
            PulledCount: 14,
            InsertedCount: 12,
            DedupedCount: 1,
            UnmappedCount: 1,
            NextCursor: "cursor_001",
            ProcessedAtUtc: now);
        PlaidBalanceRefreshResponse = new PlaidBalanceRefreshResponse(
            familyId,
            RefreshedCount: 2,
            DriftedCount: 1,
            TotalAbsoluteDrift: 4.25m,
            RefreshedAtUtc: now);
        PlaidReconciliationReportResponse = new PlaidReconciliationReportResponse(
            familyId,
            now,
            [
                new PlaidReconciliationAccountResponse(
                    accountId,
                    "Primary Checking",
                    "plaid_account_001",
                    InternalBalance: 1250.00m,
                    ProviderBalance: 1245.75m,
                    DriftAmount: -4.25m,
                    IsDrifted: true)
            ]);
        StripeSetupIntentResponse = new CreateStripeSetupIntentResponse(
            CustomerId: "cus_test_001",
            SetupIntentId: "seti_test_001",
            ClientSecret: "seti_secret_test_001");
        EvaluateSpendResponse = new EvaluateEnvelopeCardSpendResponse(
            IsAllowed: true,
            DenialReason: null,
            RemainingDailyLimit: 5m);
        ProviderActivityHealthResponse = new ProviderActivityHealthResponse(
            familyId,
            GeneratedAtUtc: now,
            LastPlaidTransactionSyncAtUtc: now.AddMinutes(-5),
            LastPlaidBalanceRefreshAtUtc: now.AddMinutes(-3),
            DriftedAccountCount: 1,
            TotalAbsoluteDrift: 4.25m,
            LastStripeWebhook: new StripeWebhookActivityResponse(
                ProcessingStatus: "Processed",
                EventType: "issuing_authorization.request",
                ProcessedAtUtc: now.AddMinutes(-2),
                ErrorMessage: null),
            NotificationDispatch: new SpendNotificationDispatchStatusResponse(
                Status: "Healthy",
                QueuedCount: 0,
                SentCount: 2,
                FailedCount: 0,
                LastAttemptAtUtc: now.AddMinutes(-1),
                LastErrorMessage: null),
            TraceId: "trace-test-001");
        ProviderActivityTimelineResponse = new ProviderActivityTimelineResponse(
            FamilyId: familyId,
            GeneratedAtUtc: now,
            RequestedTake: 25,
            Events:
            [
                new ProviderTimelineEventResponse(
                    Source: "StripeWebhook",
                    EventType: "issuing_authorization.request",
                    Status: "Processed",
                    OccurredAtUtc: now.AddMinutes(-2),
                    Summary: "Stripe webhook issuing_authorization.request -> Processed.",
                    Detail: null),
                new ProviderTimelineEventResponse(
                    Source: "NotificationDispatch",
                    EventType: "InApp",
                    Status: "Sent",
                    OccurredAtUtc: now.AddMinutes(-1),
                    Summary: "Spend notification via InApp -> Sent.",
                    Detail: null)
            ],
            TraceId: "trace-test-002");
        FailedNotificationDispatchEvents =
        [
            new FailedNotificationDispatchEventResponse(
                Guid.Parse("00000000-0000-0000-0000-000000000060"),
                familyId,
                "test-user",
                envelopeId,
                cardId,
                "Email",
                17.25m,
                "Test Merchant",
                "Failed",
                3,
                now.AddMinutes(-20),
                now.AddMinutes(-19),
                "Simulated delivery failure")
        ];

        PlaidLinks =
        [
            new PlaidAccountLinkResponse(
                Guid.Parse("00000000-0000-0000-0000-000000000050"),
                familyId,
                accountId,
                "plaid_account_001",
                now,
                now)
        ];

        var linkedAccount = new EnvelopeFinancialAccountResponse(
            financialAccountId,
            familyId,
            envelopeId,
            "Stripe",
            "fa_001",
            now,
            now);

        FamilyFinancialAccounts = [linkedAccount];
        _financialAccountByEnvelope[envelopeId] = linkedAccount;

        var card = BuildCard(
            cardId,
            familyId,
            envelopeId,
            financialAccountId,
            type: "Physical",
            status: "Active",
            brand: "Visa",
            last4: "4242",
            now);

        _cardsByEnvelope[envelopeId] = [card];
        _issuanceByCard[(envelopeId, cardId)] = BuildIssuance(
            familyId,
            envelopeId,
            card,
            shipmentStatus: "InTransit",
            now);
        _controlsByCard[(envelopeId, cardId)] = null;
        _auditByCard[(envelopeId, cardId)] = [];
    }

    public FamilyFinancialStatusResponse StatusResponse { get; set; }

    public FamilyFinancialStatusResponse ExchangeStatusResponse { get; set; }

    public NotificationPreferenceResponse NotificationPreferenceResponse { get; private set; }

    public RewrapProviderSecretsResponse RewrapProviderSecretsResponse { get; set; }

    public CreatePlaidLinkTokenResponse PlaidLinkTokenResponse { get; set; }

    public PlaidTransactionSyncResponse PlaidSyncResponse { get; set; }

    public PlaidBalanceRefreshResponse PlaidBalanceRefreshResponse { get; set; }

    public PlaidReconciliationReportResponse PlaidReconciliationReportResponse { get; set; }

    public CreateStripeSetupIntentResponse StripeSetupIntentResponse { get; set; }

    public EvaluateEnvelopeCardSpendResponse EvaluateSpendResponse { get; set; }

    public ProviderActivityHealthResponse ProviderActivityHealthResponse { get; set; }

    public ProviderActivityTimelineResponse ProviderActivityTimelineResponse { get; set; }

    public IReadOnlyList<FailedNotificationDispatchEventResponse> FailedNotificationDispatchEvents { get; private set; }

    public IReadOnlyList<PlaidAccountLinkResponse> PlaidLinks { get; private set; }

    public IReadOnlyList<EnvelopeFinancialAccountResponse> FamilyFinancialAccounts { get; private set; }

    public bool ThrowOnRefreshPlaidBalances { get; set; }

    public int CreatePlaidLinkTokenCallCount { get; private set; }

    public int ExchangePlaidPublicTokenCallCount { get; private set; }

    public int CreateStripeSetupIntentCallCount { get; private set; }

    public int DeletePlaidAccountLinkCallCount { get; private set; }

    public int RetryFailedNotificationDispatchEventCallCount { get; private set; }

    public int RewrapProviderSecretsCallCount { get; private set; }

    public int FreezeCardCallCount { get; private set; }

    public int UnfreezeCardCallCount { get; private set; }

    public int CancelCardCallCount { get; private set; }

    public Task<FamilyFinancialStatusResponse> GetFamilyFinancialStatusAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(StatusResponse);
    }

    public Task<ProviderActivityHealthResponse> GetProviderActivityHealthAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ProviderActivityHealthResponse);
    }

    public Task<ProviderActivityTimelineResponse> GetProviderActivityTimelineAsync(
        int take = 25,
        CancellationToken cancellationToken = default)
    {
        var bounded = take <= 0 ? ProviderActivityTimelineResponse.RequestedTake : take;
        var events = ProviderActivityTimelineResponse.Events.Take(bounded).ToArray();
        var response = ProviderActivityTimelineResponse with
        {
            RequestedTake = bounded,
            Events = events
        };
        return Task.FromResult(response);
    }

    public Task<NotificationPreferenceResponse> GetNotificationPreferenceAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(NotificationPreferenceResponse);
    }

    public Task<NotificationPreferenceResponse> UpdateNotificationPreferenceAsync(
        bool emailEnabled,
        bool inAppEnabled,
        bool smsEnabled,
        CancellationToken cancellationToken = default)
    {
        NotificationPreferenceResponse = NotificationPreferenceResponse with
        {
            EmailEnabled = emailEnabled,
            InAppEnabled = inAppEnabled,
            SmsEnabled = smsEnabled,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        return Task.FromResult(NotificationPreferenceResponse);
    }

    public Task<IReadOnlyList<FailedNotificationDispatchEventResponse>> ListFailedNotificationDispatchEventsAsync(
        int take = 25,
        CancellationToken cancellationToken = default)
    {
        var bounded = take <= 0 ? FailedNotificationDispatchEvents.Count : take;
        return Task.FromResult<IReadOnlyList<FailedNotificationDispatchEventResponse>>(
            FailedNotificationDispatchEvents.Take(bounded).ToArray());
    }

    public Task<RetryNotificationDispatchEventResponse> RetryFailedNotificationDispatchEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        RetryFailedNotificationDispatchEventCallCount += 1;

        var existing = FailedNotificationDispatchEvents.FirstOrDefault(evt => evt.Id == eventId);
        if (existing is null)
        {
            throw new InvalidOperationException("Failed notification event was not found.");
        }

        FailedNotificationDispatchEvents = FailedNotificationDispatchEvents
            .Where(evt => evt.Id != eventId)
            .ToArray();

        return Task.FromResult(new RetryNotificationDispatchEventResponse(
            existing.Id,
            existing.FamilyId,
            "Sent",
            existing.AttemptCount + 1,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null));
    }

    public Task<RewrapProviderSecretsResponse> RewrapProviderSecretsAsync(CancellationToken cancellationToken = default)
    {
        RewrapProviderSecretsCallCount += 1;
        RewrapProviderSecretsResponse = RewrapProviderSecretsResponse with
        {
            ExecutedAtUtc = DateTimeOffset.UtcNow
        };
        return Task.FromResult(RewrapProviderSecretsResponse);
    }

    public Task<CreatePlaidLinkTokenResponse> CreatePlaidLinkTokenAsync(
        string? clientName,
        CancellationToken cancellationToken = default)
    {
        CreatePlaidLinkTokenCallCount += 1;
        return Task.FromResult(PlaidLinkTokenResponse);
    }

    public Task<FamilyFinancialStatusResponse> ExchangePlaidPublicTokenAsync(
        string publicToken,
        CancellationToken cancellationToken = default)
    {
        ExchangePlaidPublicTokenCallCount += 1;
        StatusResponse = ExchangeStatusResponse;
        return Task.FromResult(ExchangeStatusResponse);
    }

    public Task<PlaidAccountLinkResponse> UpsertPlaidAccountLinkAsync(
        Guid accountId,
        string plaidAccountId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var existing = PlaidLinks.FirstOrDefault(x => x.AccountId == accountId);
        var updated = existing is null
            ? new PlaidAccountLinkResponse(Guid.NewGuid(), _familyId, accountId, plaidAccountId, now, now)
            : existing with { PlaidAccountId = plaidAccountId, UpdatedAtUtc = now };

        PlaidLinks = PlaidLinks
            .Where(x => x.AccountId != accountId)
            .Append(updated)
            .ToArray();

        return Task.FromResult(updated);
    }

    public Task<IReadOnlyList<PlaidAccountLinkResponse>> ListPlaidAccountLinksAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(PlaidLinks);
    }

    public Task DeletePlaidAccountLinkAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        DeletePlaidAccountLinkCallCount += 1;
        PlaidLinks = PlaidLinks.Where(link => link.Id != linkId).ToArray();
        return Task.CompletedTask;
    }

    public Task<PlaidTransactionSyncResponse> SyncPlaidTransactionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(PlaidSyncResponse);
    }

    public Task<PlaidBalanceRefreshResponse> RefreshPlaidBalancesAsync(CancellationToken cancellationToken = default)
    {
        if (ThrowOnRefreshPlaidBalances)
        {
            throw new InvalidOperationException("simulated Plaid balance refresh failure");
        }

        return Task.FromResult(PlaidBalanceRefreshResponse);
    }

    public Task<PlaidReconciliationReportResponse> GetPlaidReconciliationReportAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(PlaidReconciliationReportResponse);
    }

    public Task<CreateStripeSetupIntentResponse> CreateStripeSetupIntentAsync(
        string email,
        string? name,
        CancellationToken cancellationToken = default)
    {
        CreateStripeSetupIntentCallCount += 1;
        return Task.FromResult(StripeSetupIntentResponse);
    }

    public Task<EnvelopeFinancialAccountResponse> LinkStripeEnvelopeFinancialAccountAsync(
        Guid envelopeId,
        string? displayName,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var linked = new EnvelopeFinancialAccountResponse(
            Guid.NewGuid(),
            _familyId,
            envelopeId,
            "Stripe",
            $"fa_{envelopeId:N}"[..16],
            now,
            now);

        _financialAccountByEnvelope[envelopeId] = linked;
        FamilyFinancialAccounts = FamilyFinancialAccounts
            .Where(x => x.EnvelopeId != envelopeId)
            .Append(linked)
            .ToArray();

        return Task.FromResult(linked);
    }

    public Task<EnvelopeFinancialAccountResponse?> GetEnvelopeFinancialAccountAsync(
        Guid envelopeId,
        CancellationToken cancellationToken = default)
    {
        _financialAccountByEnvelope.TryGetValue(envelopeId, out var account);
        return Task.FromResult(account);
    }

    public Task<IReadOnlyList<EnvelopeFinancialAccountResponse>> ListFamilyFinancialAccountsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(FamilyFinancialAccounts);
    }

    public Task<EnvelopePaymentCardResponse> IssueVirtualCardAsync(
        Guid envelopeId,
        string? cardholderName,
        CancellationToken cancellationToken = default)
    {
        var card = BuildCard(
            Guid.NewGuid(),
            _familyId,
            envelopeId,
            _financialAccountId,
            type: "Virtual",
            status: "Active",
            brand: "Visa",
            last4: "9999",
            DateTimeOffset.UtcNow);

        if (!_cardsByEnvelope.TryGetValue(envelopeId, out var cards))
        {
            cards = [];
            _cardsByEnvelope[envelopeId] = cards;
        }

        cards.Insert(0, card);
        return Task.FromResult(card);
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
        var now = DateTimeOffset.UtcNow;
        var card = BuildCard(
            Guid.NewGuid(),
            _familyId,
            envelopeId,
            _financialAccountId,
            type: "Physical",
            status: "Active",
            brand: "Visa",
            last4: "1234",
            now);

        if (!_cardsByEnvelope.TryGetValue(envelopeId, out var cards))
        {
            cards = [];
            _cardsByEnvelope[envelopeId] = cards;
        }

        cards.Insert(0, card);
        var issuance = BuildIssuance(_familyId, envelopeId, card, "Requested", now);
        _issuanceByCard[(envelopeId, card.Id)] = issuance;
        return Task.FromResult(issuance);
    }

    public Task<IReadOnlyList<EnvelopePaymentCardResponse>> ListEnvelopeCardsAsync(
        Guid envelopeId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<EnvelopePaymentCardResponse>>(
            _cardsByEnvelope.TryGetValue(envelopeId, out var cards)
                ? cards.ToArray()
                : []);
    }

    public Task<EnvelopePaymentCardResponse> FreezeCardAsync(
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        FreezeCardCallCount += 1;
        return Task.FromResult(UpdateCardState(envelopeId, cardId, "Frozen"));
    }

    public Task<EnvelopePaymentCardResponse> UnfreezeCardAsync(
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        UnfreezeCardCallCount += 1;
        return Task.FromResult(UpdateCardState(envelopeId, cardId, "Active"));
    }

    public Task<EnvelopePaymentCardResponse> CancelCardAsync(
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        CancelCardCallCount += 1;
        return Task.FromResult(UpdateCardState(envelopeId, cardId, "Canceled"));
    }

    public Task<EnvelopePhysicalCardIssuanceResponse?> GetPhysicalCardIssuanceAsync(
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        _issuanceByCard.TryGetValue((envelopeId, cardId), out var issuance);
        return Task.FromResult(issuance);
    }

    public Task<EnvelopePhysicalCardIssuanceResponse> RefreshPhysicalCardIssuanceAsync(
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        if (!_issuanceByCard.TryGetValue((envelopeId, cardId), out var issuance) || issuance is null)
        {
            throw new InvalidOperationException("No issuance found for card.");
        }

        var refreshed = issuance with
        {
            Shipment = issuance.Shipment with
            {
                Status = "Delivered",
                UpdatedAtUtc = DateTimeOffset.UtcNow
            }
        };

        _issuanceByCard[(envelopeId, cardId)] = refreshed;
        return Task.FromResult(refreshed);
    }

    public Task<EnvelopePaymentCardControlResponse?> GetCardControlsAsync(
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        _controlsByCard.TryGetValue((envelopeId, cardId), out var control);
        return Task.FromResult(control);
    }

    public Task<EnvelopePaymentCardControlResponse> UpsertCardControlsAsync(
        Guid envelopeId,
        Guid cardId,
        decimal? dailyLimitAmount,
        IReadOnlyList<string>? allowedMerchantCategories,
        IReadOnlyList<string>? allowedMerchantNames,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        _controlsByCard.TryGetValue((envelopeId, cardId), out var previous);

        var control = new EnvelopePaymentCardControlResponse(
            previous?.Id ?? Guid.NewGuid(),
            _familyId,
            envelopeId,
            cardId,
            dailyLimitAmount,
            allowedMerchantCategories ?? [],
            allowedMerchantNames ?? [],
            previous?.CreatedAtUtc ?? now,
            now);

        _controlsByCard[(envelopeId, cardId)] = control;

        if (!_auditByCard.TryGetValue((envelopeId, cardId), out var auditEntries))
        {
            auditEntries = [];
            _auditByCard[(envelopeId, cardId)] = auditEntries;
        }

        var previousJson = previous is null ? null : JsonSerializer.Serialize(previous);
        auditEntries.Insert(0, new EnvelopePaymentCardControlAuditResponse(
            Guid.NewGuid(),
            _familyId,
            envelopeId,
            cardId,
            previous is null ? "Created" : "Updated",
            previousJson,
            JsonSerializer.Serialize(control),
            "test-user",
            now));

        return Task.FromResult(control);
    }

    public Task<IReadOnlyList<EnvelopePaymentCardControlAuditResponse>> ListCardControlAuditAsync(
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<EnvelopePaymentCardControlAuditResponse>>(
            _auditByCard.TryGetValue((envelopeId, cardId), out var auditEntries)
                ? auditEntries.ToArray()
                : []);
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
        return Task.FromResult(EvaluateSpendResponse);
    }

    private EnvelopePaymentCardResponse UpdateCardState(Guid envelopeId, Guid cardId, string status)
    {
        if (!_cardsByEnvelope.TryGetValue(envelopeId, out var cards))
        {
            throw new InvalidOperationException("No cards for envelope.");
        }

        var index = cards.FindIndex(x => x.Id == cardId);
        if (index < 0)
        {
            throw new InvalidOperationException("Card not found.");
        }

        var existing = cards[index];
        var updated = existing with
        {
            Status = status,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        cards[index] = updated;

        if (_issuanceByCard.TryGetValue((envelopeId, cardId), out var issuance) && issuance is not null)
        {
            _issuanceByCard[(envelopeId, cardId)] = issuance with { Card = updated };
        }

        return updated;
    }

    private static EnvelopePaymentCardResponse BuildCard(
        Guid cardId,
        Guid familyId,
        Guid envelopeId,
        Guid financialAccountId,
        string type,
        string status,
        string? brand,
        string? last4,
        DateTimeOffset now)
    {
        return new EnvelopePaymentCardResponse(
            cardId,
            familyId,
            envelopeId,
            financialAccountId,
            "Stripe",
            $"card_{cardId:N}"[..22],
            type,
            status,
            brand,
            last4,
            now,
            now);
    }

    private static EnvelopePhysicalCardIssuanceResponse BuildIssuance(
        Guid familyId,
        Guid envelopeId,
        EnvelopePaymentCardResponse card,
        string shipmentStatus,
        DateTimeOffset now)
    {
        var shipment = new EnvelopePaymentCardShipmentResponse(
            Guid.NewGuid(),
            familyId,
            envelopeId,
            card.Id,
            "Dragon Family",
            "123 Main St",
            null,
            "Austin",
            "TX",
            "78701",
            "US",
            shipmentStatus,
            "UPS",
            "1Z999AA10123456784",
            now,
            now);

        return new EnvelopePhysicalCardIssuanceResponse(card, shipment);
    }
}
