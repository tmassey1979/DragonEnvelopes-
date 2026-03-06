using System.IO;
using System.Security.Claims;
using DragonEnvelopes.Api.CrossCutting.Auth;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Financial;
using DragonEnvelopes.Infrastructure.Persistence;

namespace DragonEnvelopes.Api.Endpoints;

internal static class FinancialIntegrationEndpoints
{
    public static RouteGroupBuilder MapFinancialIntegrationEndpoints(this RouteGroupBuilder v1)
    {
        v1.MapPost("/webhooks/stripe", async (
                HttpRequest httpRequest,
                IStripeWebhookService stripeWebhookService,
                CancellationToken cancellationToken) =>
            {
                using var reader = new StreamReader(httpRequest.Body);
                var payload = await reader.ReadToEndAsync(cancellationToken);
                var signatureHeader = httpRequest.Headers["Stripe-Signature"].ToString();
                var result = await stripeWebhookService.ProcessAsync(payload, signatureHeader, cancellationToken);

                if (result.Outcome.Equals("InvalidSignature", StringComparison.OrdinalIgnoreCase)
                    || result.Outcome.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.Unauthorized();
                }

                if (result.Outcome.Equals("Failed", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.Problem(
                        title: "Stripe webhook processing failed.",
                        detail: result.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }

                return Results.Ok(new StripeWebhookProcessResponse(
                    result.Outcome,
                    result.EventId,
                    result.EventType,
                    result.Message));
            })
            .AllowAnonymous()
            .WithName("ProcessStripeWebhook")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/notifications/preferences", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IParentSpendNotificationService parentSpendNotificationService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var userId = user.FindFirstValue("sub");
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Results.Unauthorized();
                }

                var preference = await parentSpendNotificationService.GetPreferenceAsync(
                    familyId,
                    userId,
                    cancellationToken);
                return Results.Ok(new NotificationPreferenceResponse(
                    preference.FamilyId,
                    preference.UserId,
                    preference.EmailEnabled,
                    preference.InAppEnabled,
                    preference.SmsEnabled,
                    preference.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetNotificationPreference")
            .WithOpenApi();

        v1.MapPut("/families/{familyId:guid}/notifications/preferences", async (
                Guid familyId,
                UpdateNotificationPreferenceRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IParentSpendNotificationService parentSpendNotificationService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var userId = user.FindFirstValue("sub");
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Results.Unauthorized();
                }

                var preference = await parentSpendNotificationService.UpsertPreferenceAsync(
                    familyId,
                    userId,
                    request.EmailEnabled,
                    request.InAppEnabled,
                    request.SmsEnabled,
                    cancellationToken);
                return Results.Ok(new NotificationPreferenceResponse(
                    preference.FamilyId,
                    preference.UserId,
                    preference.EmailEnabled,
                    preference.InAppEnabled,
                    preference.SmsEnabled,
                    preference.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("UpsertNotificationPreference")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/financial/status", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IFinancialIntegrationService financialIntegrationService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var status = await financialIntegrationService.GetStatusAsync(familyId, cancellationToken);
                return Results.Ok(new FamilyFinancialStatusResponse(
                    status.FamilyId,
                    status.PlaidConnected,
                    status.PlaidItemId,
                    status.StripeConnected,
                    status.StripeCustomerId,
                    status.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetFamilyFinancialStatus")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/financial/plaid/link-token", async (
                Guid familyId,
                CreatePlaidLinkTokenRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IFinancialIntegrationService financialIntegrationService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var clientUserId = user.FindFirstValue("sub") ?? request.ClientUserId;
                var token = await financialIntegrationService.CreatePlaidLinkTokenAsync(
                    familyId,
                    clientUserId,
                    request.ClientName,
                    cancellationToken);

                return Results.Ok(new CreatePlaidLinkTokenResponse(token.LinkToken, token.ExpiresAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("CreatePlaidLinkToken")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/financial/plaid/exchange-public-token", async (
                Guid familyId,
                ExchangePlaidPublicTokenRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IFinancialIntegrationService financialIntegrationService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var status = await financialIntegrationService.ExchangePlaidPublicTokenAsync(
                    familyId,
                    request.PublicToken,
                    cancellationToken);

                return Results.Ok(new FamilyFinancialStatusResponse(
                    status.FamilyId,
                    status.PlaidConnected,
                    status.PlaidItemId,
                    status.StripeConnected,
                    status.StripeCustomerId,
                    status.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ExchangePlaidPublicToken")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/financial/plaid/account-links", async (
                Guid familyId,
                CreatePlaidAccountLinkRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IPlaidTransactionSyncService plaidTransactionSyncService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var link = await plaidTransactionSyncService.UpsertAccountLinkAsync(
                    familyId,
                    request.AccountId,
                    request.PlaidAccountId,
                    cancellationToken);
                return Results.Ok(new PlaidAccountLinkResponse(
                    link.Id,
                    link.FamilyId,
                    link.AccountId,
                    link.PlaidAccountId,
                    link.CreatedAtUtc,
                    link.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("UpsertPlaidAccountLink")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/financial/plaid/account-links", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IPlaidTransactionSyncService plaidTransactionSyncService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var links = await plaidTransactionSyncService.ListAccountLinksAsync(familyId, cancellationToken);
                return Results.Ok(links.Select(link => new PlaidAccountLinkResponse(
                    link.Id,
                    link.FamilyId,
                    link.AccountId,
                    link.PlaidAccountId,
                    link.CreatedAtUtc,
                    link.UpdatedAtUtc)).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListPlaidAccountLinks")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/financial/plaid/sync-transactions", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IPlaidTransactionSyncService plaidTransactionSyncService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var sync = await plaidTransactionSyncService.SyncFamilyAsync(familyId, cancellationToken);
                return Results.Ok(new PlaidTransactionSyncResponse(
                    sync.FamilyId,
                    sync.PulledCount,
                    sync.InsertedCount,
                    sync.DedupedCount,
                    sync.UnmappedCount,
                    sync.NextCursor,
                    sync.ProcessedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("SyncPlaidTransactions")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/financial/plaid/refresh-balances", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IPlaidBalanceReconciliationService plaidBalanceReconciliationService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var refresh = await plaidBalanceReconciliationService.RefreshFamilyBalancesAsync(familyId, cancellationToken);
                return Results.Ok(new PlaidBalanceRefreshResponse(
                    refresh.FamilyId,
                    refresh.RefreshedCount,
                    refresh.DriftedCount,
                    refresh.TotalAbsoluteDrift,
                    refresh.RefreshedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("RefreshPlaidBalances")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/financial/plaid/reconciliation", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IPlaidBalanceReconciliationService plaidBalanceReconciliationService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var report = await plaidBalanceReconciliationService.GetReconciliationReportAsync(familyId, cancellationToken);
                return Results.Ok(new PlaidReconciliationReportResponse(
                    report.FamilyId,
                    report.GeneratedAtUtc,
                    report.Accounts.Select(account => new PlaidReconciliationAccountResponse(
                        account.AccountId,
                        account.AccountName,
                        account.PlaidAccountId,
                        account.InternalBalance,
                        account.ProviderBalance,
                        account.DriftAmount,
                        account.IsDrifted)).ToArray()));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetPlaidReconciliationReport")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/financial/stripe/setup-intent", async (
                Guid familyId,
                CreateStripeSetupIntentRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IFinancialIntegrationService financialIntegrationService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var setupIntent = await financialIntegrationService.CreateStripeSetupIntentAsync(
                    familyId,
                    request.Email,
                    request.Name,
                    cancellationToken);

                return Results.Ok(new CreateStripeSetupIntentResponse(
                    setupIntent.CustomerId,
                    setupIntent.SetupIntentId,
                    setupIntent.ClientSecret));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("CreateStripeSetupIntent")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/envelopes/{envelopeId:guid}/financial-accounts/stripe", async (
                Guid familyId,
                Guid envelopeId,
                CreateStripeEnvelopeFinancialAccountRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopeFinancialAccountService envelopeFinancialAccountService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var account = await envelopeFinancialAccountService.LinkStripeFinancialAccountAsync(
                    familyId,
                    envelopeId,
                    request.DisplayName,
                    cancellationToken);

                return Results.Ok(new EnvelopeFinancialAccountResponse(
                    account.Id,
                    account.FamilyId,
                    account.EnvelopeId,
                    account.Provider,
                    account.ProviderFinancialAccountId,
                    account.CreatedAtUtc,
                    account.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("LinkStripeEnvelopeFinancialAccount")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/envelopes/{envelopeId:guid}/financial-account", async (
                Guid familyId,
                Guid envelopeId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopeFinancialAccountService envelopeFinancialAccountService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var account = await envelopeFinancialAccountService.GetByEnvelopeAsync(
                    familyId,
                    envelopeId,
                    cancellationToken);
                return account is null
                    ? Results.NotFound()
                    : Results.Ok(new EnvelopeFinancialAccountResponse(
                        account.Id,
                        account.FamilyId,
                        account.EnvelopeId,
                        account.Provider,
                        account.ProviderFinancialAccountId,
                        account.CreatedAtUtc,
                        account.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetEnvelopeFinancialAccount")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/financial-accounts", async (
                Guid familyId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopeFinancialAccountService envelopeFinancialAccountService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var accounts = await envelopeFinancialAccountService.ListByFamilyAsync(familyId, cancellationToken);
                return Results.Ok(accounts.Select(account => new EnvelopeFinancialAccountResponse(
                    account.Id,
                    account.FamilyId,
                    account.EnvelopeId,
                    account.Provider,
                    account.ProviderFinancialAccountId,
                    account.CreatedAtUtc,
                    account.UpdatedAtUtc)).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListFamilyFinancialAccounts")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/virtual", async (
                Guid familyId,
                Guid envelopeId,
                CreateVirtualEnvelopeCardRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardService envelopePaymentCardService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var card = await envelopePaymentCardService.IssueVirtualCardAsync(
                    familyId,
                    envelopeId,
                    request.CardholderName,
                    cancellationToken);
                return Results.Ok(new EnvelopePaymentCardResponse(
                    card.Id,
                    card.FamilyId,
                    card.EnvelopeId,
                    card.EnvelopeFinancialAccountId,
                    card.Provider,
                    card.ProviderCardId,
                    card.Type,
                    card.Status,
                    card.Brand,
                    card.Last4,
                    card.CreatedAtUtc,
                    card.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("IssueVirtualEnvelopeCard")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/physical", async (
                Guid familyId,
                Guid envelopeId,
                CreatePhysicalEnvelopeCardRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardService envelopePaymentCardService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var issuance = await envelopePaymentCardService.IssuePhysicalCardAsync(
                    familyId,
                    envelopeId,
                    request.CardholderName,
                    request.RecipientName,
                    request.AddressLine1,
                    request.AddressLine2,
                    request.City,
                    request.StateOrProvince,
                    request.PostalCode,
                    request.CountryCode,
                    cancellationToken);

                return Results.Ok(new EnvelopePhysicalCardIssuanceResponse(
                    new EnvelopePaymentCardResponse(
                        issuance.Card.Id,
                        issuance.Card.FamilyId,
                        issuance.Card.EnvelopeId,
                        issuance.Card.EnvelopeFinancialAccountId,
                        issuance.Card.Provider,
                        issuance.Card.ProviderCardId,
                        issuance.Card.Type,
                        issuance.Card.Status,
                        issuance.Card.Brand,
                        issuance.Card.Last4,
                        issuance.Card.CreatedAtUtc,
                        issuance.Card.UpdatedAtUtc),
                    new EnvelopePaymentCardShipmentResponse(
                        issuance.Shipment.Id,
                        issuance.Shipment.FamilyId,
                        issuance.Shipment.EnvelopeId,
                        issuance.Shipment.CardId,
                        issuance.Shipment.RecipientName,
                        issuance.Shipment.AddressLine1,
                        issuance.Shipment.AddressLine2,
                        issuance.Shipment.City,
                        issuance.Shipment.StateOrProvince,
                        issuance.Shipment.PostalCode,
                        issuance.Shipment.CountryCode,
                        issuance.Shipment.Status,
                        issuance.Shipment.Carrier,
                        issuance.Shipment.TrackingNumber,
                        issuance.Shipment.RequestedAtUtc,
                        issuance.Shipment.UpdatedAtUtc)));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("IssuePhysicalEnvelopeCard")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards", async (
                Guid familyId,
                Guid envelopeId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardService envelopePaymentCardService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var cards = await envelopePaymentCardService.ListByEnvelopeAsync(
                    familyId,
                    envelopeId,
                    cancellationToken);
                return Results.Ok(cards.Select(card => new EnvelopePaymentCardResponse(
                    card.Id,
                    card.FamilyId,
                    card.EnvelopeId,
                    card.EnvelopeFinancialAccountId,
                    card.Provider,
                    card.ProviderCardId,
                    card.Type,
                    card.Status,
                    card.Brand,
                    card.Last4,
                    card.CreatedAtUtc,
                    card.UpdatedAtUtc)).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListEnvelopeCards")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/{cardId:guid}/freeze", async (
                Guid familyId,
                Guid envelopeId,
                Guid cardId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardService envelopePaymentCardService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var card = await envelopePaymentCardService.FreezeCardAsync(
                    familyId,
                    envelopeId,
                    cardId,
                    cancellationToken);
                return Results.Ok(new EnvelopePaymentCardResponse(
                    card.Id,
                    card.FamilyId,
                    card.EnvelopeId,
                    card.EnvelopeFinancialAccountId,
                    card.Provider,
                    card.ProviderCardId,
                    card.Type,
                    card.Status,
                    card.Brand,
                    card.Last4,
                    card.CreatedAtUtc,
                    card.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("FreezeEnvelopeCard")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/{cardId:guid}/unfreeze", async (
                Guid familyId,
                Guid envelopeId,
                Guid cardId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardService envelopePaymentCardService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var card = await envelopePaymentCardService.UnfreezeCardAsync(
                    familyId,
                    envelopeId,
                    cardId,
                    cancellationToken);
                return Results.Ok(new EnvelopePaymentCardResponse(
                    card.Id,
                    card.FamilyId,
                    card.EnvelopeId,
                    card.EnvelopeFinancialAccountId,
                    card.Provider,
                    card.ProviderCardId,
                    card.Type,
                    card.Status,
                    card.Brand,
                    card.Last4,
                    card.CreatedAtUtc,
                    card.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("UnfreezeEnvelopeCard")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/{cardId:guid}/cancel", async (
                Guid familyId,
                Guid envelopeId,
                Guid cardId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardService envelopePaymentCardService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var card = await envelopePaymentCardService.CancelCardAsync(
                    familyId,
                    envelopeId,
                    cardId,
                    cancellationToken);
                return Results.Ok(new EnvelopePaymentCardResponse(
                    card.Id,
                    card.FamilyId,
                    card.EnvelopeId,
                    card.EnvelopeFinancialAccountId,
                    card.Provider,
                    card.ProviderCardId,
                    card.Type,
                    card.Status,
                    card.Brand,
                    card.Last4,
                    card.CreatedAtUtc,
                    card.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("CancelEnvelopeCard")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/{cardId:guid}/issuance", async (
                Guid familyId,
                Guid envelopeId,
                Guid cardId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardService envelopePaymentCardService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var issuance = await envelopePaymentCardService.GetPhysicalCardIssuanceAsync(
                    familyId,
                    envelopeId,
                    cardId,
                    cancellationToken);

                return issuance is null
                    ? Results.NotFound()
                    : Results.Ok(new EnvelopePhysicalCardIssuanceResponse(
                        new EnvelopePaymentCardResponse(
                            issuance.Card.Id,
                            issuance.Card.FamilyId,
                            issuance.Card.EnvelopeId,
                            issuance.Card.EnvelopeFinancialAccountId,
                            issuance.Card.Provider,
                            issuance.Card.ProviderCardId,
                            issuance.Card.Type,
                            issuance.Card.Status,
                            issuance.Card.Brand,
                            issuance.Card.Last4,
                            issuance.Card.CreatedAtUtc,
                            issuance.Card.UpdatedAtUtc),
                        new EnvelopePaymentCardShipmentResponse(
                            issuance.Shipment.Id,
                            issuance.Shipment.FamilyId,
                            issuance.Shipment.EnvelopeId,
                            issuance.Shipment.CardId,
                            issuance.Shipment.RecipientName,
                            issuance.Shipment.AddressLine1,
                            issuance.Shipment.AddressLine2,
                            issuance.Shipment.City,
                            issuance.Shipment.StateOrProvince,
                            issuance.Shipment.PostalCode,
                            issuance.Shipment.CountryCode,
                            issuance.Shipment.Status,
                            issuance.Shipment.Carrier,
                            issuance.Shipment.TrackingNumber,
                            issuance.Shipment.RequestedAtUtc,
                            issuance.Shipment.UpdatedAtUtc)));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetEnvelopePhysicalCardIssuance")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/{cardId:guid}/issuance/refresh", async (
                Guid familyId,
                Guid envelopeId,
                Guid cardId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardService envelopePaymentCardService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var issuance = await envelopePaymentCardService.RefreshPhysicalCardIssuanceStatusAsync(
                    familyId,
                    envelopeId,
                    cardId,
                    cancellationToken);

                return Results.Ok(new EnvelopePhysicalCardIssuanceResponse(
                    new EnvelopePaymentCardResponse(
                        issuance.Card.Id,
                        issuance.Card.FamilyId,
                        issuance.Card.EnvelopeId,
                        issuance.Card.EnvelopeFinancialAccountId,
                        issuance.Card.Provider,
                        issuance.Card.ProviderCardId,
                        issuance.Card.Type,
                        issuance.Card.Status,
                        issuance.Card.Brand,
                        issuance.Card.Last4,
                        issuance.Card.CreatedAtUtc,
                        issuance.Card.UpdatedAtUtc),
                    new EnvelopePaymentCardShipmentResponse(
                        issuance.Shipment.Id,
                        issuance.Shipment.FamilyId,
                        issuance.Shipment.EnvelopeId,
                        issuance.Shipment.CardId,
                        issuance.Shipment.RecipientName,
                        issuance.Shipment.AddressLine1,
                        issuance.Shipment.AddressLine2,
                        issuance.Shipment.City,
                        issuance.Shipment.StateOrProvince,
                        issuance.Shipment.PostalCode,
                        issuance.Shipment.CountryCode,
                        issuance.Shipment.Status,
                        issuance.Shipment.Carrier,
                        issuance.Shipment.TrackingNumber,
                        issuance.Shipment.RequestedAtUtc,
                        issuance.Shipment.UpdatedAtUtc)));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("RefreshEnvelopePhysicalCardIssuance")
            .WithOpenApi();

        v1.MapPut("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/{cardId:guid}/controls", async (
                Guid familyId,
                Guid envelopeId,
                Guid cardId,
                UpsertEnvelopePaymentCardControlRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardControlService envelopePaymentCardControlService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var control = await envelopePaymentCardControlService.UpsertControlsAsync(
                    familyId,
                    envelopeId,
                    cardId,
                    request.DailyLimitAmount,
                    request.AllowedMerchantCategories,
                    request.AllowedMerchantNames,
                    user.FindFirstValue("sub"),
                    cancellationToken);
                return Results.Ok(new EnvelopePaymentCardControlResponse(
                    control.Id,
                    control.FamilyId,
                    control.EnvelopeId,
                    control.CardId,
                    control.DailyLimitAmount,
                    control.AllowedMerchantCategories,
                    control.AllowedMerchantNames,
                    control.CreatedAtUtc,
                    control.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("UpsertEnvelopeCardControls")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/{cardId:guid}/controls", async (
                Guid familyId,
                Guid envelopeId,
                Guid cardId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardControlService envelopePaymentCardControlService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var control = await envelopePaymentCardControlService.GetByCardAsync(
                    familyId,
                    envelopeId,
                    cardId,
                    cancellationToken);

                return control is null
                    ? Results.NotFound()
                    : Results.Ok(new EnvelopePaymentCardControlResponse(
                        control.Id,
                        control.FamilyId,
                        control.EnvelopeId,
                        control.CardId,
                        control.DailyLimitAmount,
                        control.AllowedMerchantCategories,
                        control.AllowedMerchantNames,
                        control.CreatedAtUtc,
                        control.UpdatedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetEnvelopeCardControls")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/{cardId:guid}/controls/audit", async (
                Guid familyId,
                Guid envelopeId,
                Guid cardId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardControlService envelopePaymentCardControlService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var audit = await envelopePaymentCardControlService.ListAuditByCardAsync(
                    familyId,
                    envelopeId,
                    cardId,
                    cancellationToken);

                return Results.Ok(audit.Select(entry => new EnvelopePaymentCardControlAuditResponse(
                    entry.Id,
                    entry.FamilyId,
                    entry.EnvelopeId,
                    entry.CardId,
                    entry.Action,
                    entry.PreviousStateJson,
                    entry.NewStateJson,
                    entry.ChangedBy,
                    entry.ChangedAtUtc)).ToArray());
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListEnvelopeCardControlAudit")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/envelopes/{envelopeId:guid}/cards/{cardId:guid}/controls/evaluate", async (
                Guid familyId,
                Guid envelopeId,
                Guid cardId,
                EvaluateEnvelopeCardSpendRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IEnvelopePaymentCardControlService envelopePaymentCardControlService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var evaluation = await envelopePaymentCardControlService.EvaluateSpendAsync(
                    familyId,
                    envelopeId,
                    cardId,
                    request.MerchantName,
                    request.MerchantCategory,
                    request.Amount,
                    request.SpentTodayAmount,
                    cancellationToken);

                return Results.Ok(new EvaluateEnvelopeCardSpendResponse(
                    evaluation.IsAllowed,
                    evaluation.DenialReason,
                    evaluation.RemainingDailyLimit));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("EvaluateEnvelopeCardSpend")
            .WithOpenApi();

        return v1;
    }
}
