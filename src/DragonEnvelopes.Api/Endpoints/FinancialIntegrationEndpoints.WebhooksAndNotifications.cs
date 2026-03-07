using System.IO;
using System.Security.Claims;
using System.Text.Json;
using DragonEnvelopes.Api.CrossCutting.Auth;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Contracts.Financial;
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
                IPlaidTransactionSyncService plaidTransactionSyncService,
                IPlaidBalanceReconciliationService plaidBalanceReconciliationService,
                CancellationToken cancellationToken) =>
            {
                string payload;
                using (var reader = new StreamReader(httpRequest.Body))
                {
                    payload = await reader.ReadToEndAsync(cancellationToken);
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

                    if (string.IsNullOrWhiteSpace(itemId))
                    {
                        return Results.Ok(new PlaidWebhookProcessResponse(
                            Outcome: "Ignored",
                            WebhookType: webhookType,
                            WebhookCode: webhookCode,
                            ItemId: null,
                            FamilyId: null,
                            Message: "Payload does not include item_id."));
                    }

                    var normalizedItemId = itemId.Trim();
                    var familyId = await dbContext.FamilyFinancialProfiles
                        .AsNoTracking()
                        .Where(profile => profile.PlaidItemId == normalizedItemId)
                        .Select(profile => (Guid?)profile.FamilyId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (!familyId.HasValue)
                    {
                        return Results.Ok(new PlaidWebhookProcessResponse(
                            Outcome: "Ignored",
                            WebhookType: webhookType,
                            WebhookCode: webhookCode,
                            ItemId: normalizedItemId,
                            FamilyId: null,
                            Message: "No family financial profile matched item_id."));
                    }

                    try
                    {
                        if (webhookType != null && webhookType.Equals("TRANSACTIONS", StringComparison.OrdinalIgnoreCase))
                        {
                            var sync = await plaidTransactionSyncService.SyncFamilyAsync(familyId.Value, cancellationToken);
                            return Results.Ok(new PlaidWebhookProcessResponse(
                                Outcome: "Processed",
                                WebhookType: webhookType,
                                WebhookCode: webhookCode,
                                ItemId: normalizedItemId,
                                FamilyId: familyId.Value,
                                Message: $"Plaid sync processed: pulled {sync.PulledCount}, inserted {sync.InsertedCount}, deduped {sync.DedupedCount}, unmapped {sync.UnmappedCount}."));
                        }

                        if (webhookType != null && webhookType.Equals("BALANCE", StringComparison.OrdinalIgnoreCase))
                        {
                            var refresh = await plaidBalanceReconciliationService.RefreshFamilyBalancesAsync(familyId.Value, cancellationToken);
                            return Results.Ok(new PlaidWebhookProcessResponse(
                                Outcome: "Processed",
                                WebhookType: webhookType,
                                WebhookCode: webhookCode,
                                ItemId: normalizedItemId,
                                FamilyId: familyId.Value,
                                Message: $"Plaid balance refresh processed: refreshed {refresh.RefreshedCount}, drifted {refresh.DriftedCount}, absolute drift {refresh.TotalAbsoluteDrift:0.00}."));
                        }
                    }
                    catch (Exception ex)
                    {
                        return Results.Ok(new PlaidWebhookProcessResponse(
                            Outcome: "Failed",
                            WebhookType: webhookType,
                            WebhookCode: webhookCode,
                            ItemId: normalizedItemId,
                            FamilyId: familyId.Value,
                            Message: TrimActivityError(ex.Message) ?? "Plaid webhook processing failed."));
                    }

                    return Results.Ok(new PlaidWebhookProcessResponse(
                        Outcome: "Ignored",
                        WebhookType: webhookType,
                        WebhookCode: webhookCode,
                        ItemId: normalizedItemId,
                        FamilyId: familyId.Value,
                        Message: "Webhook type is not configured for automated processing."));
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
