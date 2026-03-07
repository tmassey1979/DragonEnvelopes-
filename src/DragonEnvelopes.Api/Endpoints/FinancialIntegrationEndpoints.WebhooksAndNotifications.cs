using System.IO;
using System.Security.Claims;
using System.Text.Json;
using DragonEnvelopes.Api.CrossCutting.Auth;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Financial;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Api.Endpoints;

internal static partial class FinancialIntegrationEndpoints
{
    private static void MapWebhookAndNotificationEndpoints(RouteGroupBuilder v1)
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

        v1.MapPost("/webhooks/plaid", async (
                HttpRequest httpRequest,
                DragonEnvelopesDbContext dbContext,
                IPlaidWebhookVerificationService plaidWebhookVerificationService,
                IPlaidTransactionSyncService plaidTransactionSyncService,
                IPlaidBalanceReconciliationService plaidBalanceReconciliationService,
                CancellationToken cancellationToken) =>
            {
                var receivedAtUtc = DateTimeOffset.UtcNow;
                string payload;
                using (var reader = new StreamReader(httpRequest.Body))
                {
                    payload = await reader.ReadToEndAsync(cancellationToken);
                }

                var plaidSignatureHeader = httpRequest.Headers["Plaid-Signature"].ToString();
                var verificationResult = plaidWebhookVerificationService.Verify(payload, plaidSignatureHeader);
                if (!verificationResult.IsVerified)
                {
                    if (!verificationResult.IsDisabled)
                    {
                        var verificationFailure = new PlaidWebhookEvent(
                            Guid.NewGuid(),
                            webhookType: "Unknown",
                            webhookCode: null,
                            itemId: null,
                            familyId: null,
                            processingStatus: "Failed",
                            errorMessage: TrimActivityError(verificationResult.Message),
                            payloadJson: payload,
                            receivedAtUtc,
                            DateTimeOffset.UtcNow);
                        dbContext.PlaidWebhookEvents.Add(verificationFailure);
                        await dbContext.SaveChangesAsync(cancellationToken);
                        return Results.Unauthorized();
                    }
                }

                JsonDocument document;
                try
                {
                    document = JsonDocument.Parse(payload);
                }
                catch (JsonException)
                {
                    return Results.BadRequest("Plaid webhook payload must be valid JSON.");
                }

                using (document)
                {
                    if (document.RootElement.ValueKind != JsonValueKind.Object)
                    {
                        return Results.BadRequest("Plaid webhook payload must be a JSON object.");
                    }

                    var webhookType = document.RootElement.TryGetProperty("webhook_type", out var webhookTypeElement)
                        ? webhookTypeElement.GetString()
                        : null;
                    var webhookCode = document.RootElement.TryGetProperty("webhook_code", out var webhookCodeElement)
                        ? webhookCodeElement.GetString()
                        : null;
                    var itemId = document.RootElement.TryGetProperty("item_id", out var itemIdElement)
                        ? itemIdElement.GetString()
                        : null;
                    var normalizedWebhookType = string.IsNullOrWhiteSpace(webhookType)
                        ? "Unknown"
                        : webhookType.Trim();
                    var normalizedWebhookCode = string.IsNullOrWhiteSpace(webhookCode)
                        ? null
                        : webhookCode.Trim();
                    var normalizedItemId = string.IsNullOrWhiteSpace(itemId)
                        ? null
                        : itemId.Trim();
                    // Keep a bounded dedupe window to prevent unbounded webhook lookup scans.
                    var deduplicationLookbackStart = receivedAtUtc.AddHours(-72);

                    async Task<IResult> PersistAndReturnAsync(string outcome, Guid? familyId, string? message)
                    {
                        var webhookEvent = new PlaidWebhookEvent(
                            Guid.NewGuid(),
                            normalizedWebhookType,
                            normalizedWebhookCode,
                            normalizedItemId,
                            familyId,
                            outcome,
                            outcome.Equals("Failed", StringComparison.OrdinalIgnoreCase)
                                ? TrimActivityError(message)
                                : null,
                            payload,
                            receivedAtUtc,
                            DateTimeOffset.UtcNow);

                        dbContext.PlaidWebhookEvents.Add(webhookEvent);
                        await dbContext.SaveChangesAsync(cancellationToken);

                        return Results.Ok(new PlaidWebhookProcessResponse(
                            Outcome: outcome,
                            WebhookType: webhookType,
                            WebhookCode: webhookCode,
                            ItemId: normalizedItemId,
                            FamilyId: familyId,
                            Message: message));
                    }

                    var duplicateEvent = await dbContext.PlaidWebhookEvents
                        .AsNoTracking()
                        .Where(x => x.ReceivedAtUtc >= deduplicationLookbackStart)
                        .Where(x => x.ProcessingStatus != "Duplicate")
                        .Where(x => x.WebhookType == normalizedWebhookType
                                    && x.WebhookCode == normalizedWebhookCode
                                    && x.ItemId == normalizedItemId
                                    && x.PayloadJson == payload)
                        .OrderByDescending(x => x.ReceivedAtUtc)
                        .Select(x => new { x.Id, x.FamilyId })
                        .FirstOrDefaultAsync(cancellationToken);
                    if (duplicateEvent is not null)
                    {
                        return await PersistAndReturnAsync(
                            outcome: "Duplicate",
                            familyId: duplicateEvent.FamilyId,
                            message: $"Duplicate webhook delivery suppressed. Original event id: {duplicateEvent.Id}.");
                    }

                    if (string.IsNullOrWhiteSpace(normalizedItemId))
                    {
                        return await PersistAndReturnAsync(
                            outcome: "Ignored",
                            familyId: null,
                            message: "Payload does not include item_id.");
                    }

                    var familyId = await dbContext.FamilyFinancialProfiles
                        .AsNoTracking()
                        .Where(profile => profile.PlaidItemId == normalizedItemId)
                        .Select(profile => (Guid?)profile.FamilyId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (!familyId.HasValue)
                    {
                        return await PersistAndReturnAsync(
                            outcome: "Ignored",
                            familyId: null,
                            message: "No family financial profile matched item_id.");
                    }

                    try
                    {
                        if (webhookType != null && webhookType.Equals("TRANSACTIONS", StringComparison.OrdinalIgnoreCase))
                        {
                            var sync = await plaidTransactionSyncService.SyncFamilyAsync(familyId.Value, cancellationToken);
                            return await PersistAndReturnAsync(
                                outcome: "Processed",
                                familyId: familyId.Value,
                                message: $"Plaid sync processed: pulled {sync.PulledCount}, inserted {sync.InsertedCount}, deduped {sync.DedupedCount}, unmapped {sync.UnmappedCount}.");
                        }

                        if (webhookType != null && webhookType.Equals("BALANCE", StringComparison.OrdinalIgnoreCase))
                        {
                            var refresh = await plaidBalanceReconciliationService.RefreshFamilyBalancesAsync(familyId.Value, cancellationToken);
                            return await PersistAndReturnAsync(
                                outcome: "Processed",
                                familyId: familyId.Value,
                                message: $"Plaid balance refresh processed: refreshed {refresh.RefreshedCount}, drifted {refresh.DriftedCount}, absolute drift {refresh.TotalAbsoluteDrift:0.00}.");
                        }
                    }
                    catch (Exception ex)
                    {
                        return await PersistAndReturnAsync(
                            outcome: "Failed",
                            familyId: familyId.Value,
                            message: TrimActivityError(ex.Message) ?? "Plaid webhook processing failed.");
                    }

                    return await PersistAndReturnAsync(
                        outcome: "Ignored",
                        familyId: familyId.Value,
                        message: "Webhook type is not configured for automated processing.");
                }
            })
            .AllowAnonymous()
            .WithName("ProcessPlaidWebhook")
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

        v1.MapGet("/families/{familyId:guid}/notifications/dispatch-events/failed", async (
                Guid familyId,
                int? take,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ISpendNotificationDispatchService spendNotificationDispatchService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var failedEvents = await spendNotificationDispatchService.ListFailedEventsAsync(
                    familyId,
                    take ?? 25,
                    cancellationToken);
                return Results.Ok(failedEvents.Select(static evt => new FailedNotificationDispatchEventResponse(
                    evt.Id,
                    evt.FamilyId,
                    evt.UserId,
                    evt.EnvelopeId,
                    evt.CardId,
                    evt.Channel,
                    evt.Amount,
                    evt.Merchant,
                    evt.Status,
                    evt.AttemptCount,
                    evt.CreatedAtUtc,
                    evt.LastAttemptAtUtc,
                    evt.ErrorMessage)));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ListFailedNotificationDispatchEvents")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/notifications/dispatch-events/{eventId:guid}/retry", async (
                Guid familyId,
                Guid eventId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ISpendNotificationDispatchService spendNotificationDispatchService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var retried = await spendNotificationDispatchService.RetryFailedEventAsync(
                    familyId,
                    eventId,
                    cancellationToken);
                return Results.Ok(new RetryNotificationDispatchEventResponse(
                    retried.Id,
                    retried.FamilyId,
                    retried.Status,
                    retried.AttemptCount,
                    retried.LastAttemptAtUtc,
                    retried.SentAtUtc,
                    retried.ErrorMessage));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("RetryNotificationDispatchEvent")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/financial/provider-activity/timeline/notifications/{eventId:guid}/replay", async (
                Guid familyId,
                Guid eventId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                ISpendNotificationDispatchService spendNotificationDispatchService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var replayed = await spendNotificationDispatchService.ReplayEventAsync(
                    familyId,
                    eventId,
                    cancellationToken);
                return Results.Ok(new RetryNotificationDispatchEventResponse(
                    replayed.Id,
                    replayed.FamilyId,
                    replayed.Status,
                    replayed.AttemptCount,
                    replayed.LastAttemptAtUtc,
                    replayed.SentAtUtc,
                    replayed.ErrorMessage));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("ReplayTimelineNotificationDispatchEvent")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/financial/provider-activity/timeline/stripe-webhooks/{eventId:guid}/replay", async (
                Guid familyId,
                Guid eventId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IStripeWebhookService stripeWebhookService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var exists = await dbContext.StripeWebhookEvents
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == eventId && x.FamilyId == familyId, cancellationToken);
                if (!exists)
                {
                    return Results.NotFound();
                }

                var replayed = await stripeWebhookService.ReplayFailedEventAsync(
                    familyId,
                    eventId,
                    cancellationToken);
                return Results.Ok(new ReplayStripeWebhookEventResponse(
                    replayed.Id,
                    replayed.FamilyId,
                    replayed.Status,
                    replayed.Outcome,
                    replayed.ProcessedAtUtc,
                    replayed.ErrorMessage));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.Parent)
            .WithName("ReplayTimelineStripeWebhookEvent")
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
                    status.UpdatedAtUtc,
                    status.ReconciliationDriftThreshold));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetFamilyFinancialStatus")
            .WithOpenApi();

        v1.MapPut("/families/{familyId:guid}/financial/reconciliation-threshold", async (
                Guid familyId,
                UpdateReconciliationDriftThresholdRequest request,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                IFinancialIntegrationService financialIntegrationService,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var status = await financialIntegrationService.UpdateReconciliationDriftThresholdAsync(
                    familyId,
                    request.ReconciliationDriftThreshold,
                    cancellationToken);

                return Results.Ok(new FamilyFinancialStatusResponse(
                    status.FamilyId,
                    status.PlaidConnected,
                    status.PlaidItemId,
                    status.StripeConnected,
                    status.StripeCustomerId,
                    status.UpdatedAtUtc,
                    status.ReconciliationDriftThreshold));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.Parent)
            .WithName("UpdateReconciliationDriftThreshold")
            .WithOpenApi();

        v1.MapPost("/families/{familyId:guid}/financial/security/rewrap-provider-secrets", async (
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

                var result = await financialIntegrationService.RewrapProviderSecretsAsync(
                    familyId,
                    cancellationToken);

                return Results.Ok(new RewrapProviderSecretsResponse(
                    result.FamilyId,
                    result.ProfileFound,
                    result.FieldsTouched,
                    result.ExecutedAtUtc));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.Parent)
            .WithName("RewrapProviderSecrets")
            .WithOpenApi();
    }
}
