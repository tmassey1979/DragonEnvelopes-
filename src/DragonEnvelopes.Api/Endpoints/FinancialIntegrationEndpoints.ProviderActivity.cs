using System.Security.Claims;
using DragonEnvelopes.Api.CrossCutting.Auth;
using DragonEnvelopes.Contracts.Financial;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DragonEnvelopes.Api.Endpoints;

internal static partial class FinancialIntegrationEndpoints
{
    private static void MapProviderActivityEndpoints(RouteGroupBuilder v1)
    {
        v1.MapGet("/families/{familyId:guid}/financial/provider-activity", async (
                Guid familyId,
                ClaimsPrincipal user,
                HttpContext httpContext,
                DragonEnvelopesDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var plaidSyncCursor = await dbContext.PlaidSyncCursors
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.FamilyId == familyId, cancellationToken);

                var latestPlaidBalanceRefreshAtUtc = await dbContext.PlaidBalanceSnapshots
                    .AsNoTracking()
                    .Where(x => x.FamilyId == familyId)
                    .OrderByDescending(x => x.RefreshedAtUtc)
                    .Select(static snapshot => (DateTimeOffset?)snapshot.RefreshedAtUtc)
                    .FirstOrDefaultAsync(cancellationToken);

                var driftedAccountCount = 0;
                var totalAbsoluteDrift = 0m;
                if (latestPlaidBalanceRefreshAtUtc.HasValue)
                {
                    var latestSnapshots = await dbContext.PlaidBalanceSnapshots
                        .AsNoTracking()
                        .Where(snapshot =>
                            snapshot.FamilyId == familyId
                            && snapshot.RefreshedAtUtc == latestPlaidBalanceRefreshAtUtc.Value)
                        .ToArrayAsync(cancellationToken);

                    driftedAccountCount = latestSnapshots.Count(snapshot => snapshot.DriftAmount != 0m);
                    totalAbsoluteDrift = latestSnapshots.Sum(snapshot => decimal.Abs(snapshot.DriftAmount));
                }

                var lastWebhook = await dbContext.StripeWebhookEvents
                    .AsNoTracking()
                    .Where(x => x.FamilyId == familyId)
                    .OrderByDescending(x => x.ProcessedAtUtc)
                    .FirstOrDefaultAsync(cancellationToken);

                var notificationEvents = await dbContext.SpendNotificationEvents
                    .AsNoTracking()
                    .Where(x => x.FamilyId == familyId)
                    .ToArrayAsync(cancellationToken);

                var queuedCount = notificationEvents.Count(x => x.Status.Equals("Queued", StringComparison.OrdinalIgnoreCase));
                var sentCount = notificationEvents.Count(x => x.Status.Equals("Sent", StringComparison.OrdinalIgnoreCase));
                var failedCount = notificationEvents.Count(x => x.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase));
                var lastNotification = notificationEvents
                    .OrderByDescending(x => x.LastAttemptAtUtc ?? x.CreatedAtUtc)
                    .FirstOrDefault();
                var dispatchStatus = failedCount > 0
                    ? "Degraded"
                    : queuedCount > 0
                        ? "Pending"
                        : sentCount > 0
                            ? "Healthy"
                            : "NoEvents";

                return Results.Ok(new ProviderActivityHealthResponse(
                    FamilyId: familyId,
                    GeneratedAtUtc: DateTimeOffset.UtcNow,
                    LastPlaidTransactionSyncAtUtc: plaidSyncCursor?.UpdatedAtUtc,
                    LastPlaidBalanceRefreshAtUtc: latestPlaidBalanceRefreshAtUtc,
                    DriftedAccountCount: driftedAccountCount,
                    TotalAbsoluteDrift: totalAbsoluteDrift,
                    LastStripeWebhook: lastWebhook is null
                        ? null
                        : new StripeWebhookActivityResponse(
                            lastWebhook.ProcessingStatus,
                            lastWebhook.EventType,
                            lastWebhook.ProcessedAtUtc,
                            TrimActivityError(lastWebhook.ErrorMessage)),
                    NotificationDispatch: new SpendNotificationDispatchStatusResponse(
                        dispatchStatus,
                        queuedCount,
                        sentCount,
                        failedCount,
                        lastNotification?.LastAttemptAtUtc ?? lastNotification?.CreatedAtUtc,
                        TrimActivityError(lastNotification?.ErrorMessage)),
                    TraceId: httpContext.TraceIdentifier));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetProviderActivityHealth")
            .WithOpenApi();

        v1.MapGet("/families/{familyId:guid}/financial/provider-activity/timeline", async (
                Guid familyId,
                int? take,
                string? source,
                string? status,
                ClaimsPrincipal user,
                HttpContext httpContext,
                DragonEnvelopesDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var normalizedTake = Math.Clamp(take ?? 25, 1, 100);
                var normalizedSource = string.IsNullOrWhiteSpace(source)
                    ? null
                    : source.Trim();
                if (!string.IsNullOrWhiteSpace(normalizedSource)
                    && !normalizedSource.Equals("StripeWebhook", StringComparison.OrdinalIgnoreCase)
                    && !normalizedSource.Equals("PlaidWebhook", StringComparison.OrdinalIgnoreCase)
                    && !normalizedSource.Equals("NotificationDispatch", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest("source must be StripeWebhook, PlaidWebhook, or NotificationDispatch.");
                }

                var includeWebhooks = string.IsNullOrWhiteSpace(normalizedSource)
                    || normalizedSource.Equals("StripeWebhook", StringComparison.OrdinalIgnoreCase);
                var includePlaidWebhooks = string.IsNullOrWhiteSpace(normalizedSource)
                    || normalizedSource.Equals("PlaidWebhook", StringComparison.OrdinalIgnoreCase);
                var includeNotifications = string.IsNullOrWhiteSpace(normalizedSource)
                    || normalizedSource.Equals("NotificationDispatch", StringComparison.OrdinalIgnoreCase);
                var normalizedStatus = string.IsNullOrWhiteSpace(status)
                    ? null
                    : status.Trim();
                var normalizedStatusLower = normalizedStatus?.ToLowerInvariant();

                ProviderTimelineEventResponse[] webhooks = [];
                if (includeWebhooks)
                {
                    var webhookQuery = dbContext.StripeWebhookEvents
                        .AsNoTracking()
                        .Where(x => x.FamilyId == familyId);

                    if (!string.IsNullOrWhiteSpace(normalizedStatus))
                    {
                        webhookQuery = webhookQuery
                            .Where(x => x.ProcessingStatus != null && x.ProcessingStatus.ToLower() == normalizedStatusLower);
                    }

                    webhooks = await webhookQuery
                        .OrderByDescending(x => x.ProcessedAtUtc)
                        .Take(normalizedTake)
                        .Select(static webhook => new ProviderTimelineEventResponse(
                            "StripeWebhook",
                            webhook.EventType,
                            webhook.ProcessingStatus,
                            webhook.ProcessedAtUtc,
                            $"Stripe webhook {webhook.EventType} -> {webhook.ProcessingStatus}.",
                            webhook.ErrorMessage,
                            webhook.Id,
                            null))
                        .ToArrayAsync(cancellationToken);
                }

                ProviderTimelineEventResponse[] plaidWebhooks = [];
                if (includePlaidWebhooks)
                {
                    var plaidWebhookQuery = dbContext.PlaidWebhookEvents
                        .AsNoTracking()
                        .Where(x => x.FamilyId == familyId);

                    if (!string.IsNullOrWhiteSpace(normalizedStatus))
                    {
                        plaidWebhookQuery = plaidWebhookQuery
                            .Where(x => x.ProcessingStatus != null && x.ProcessingStatus.ToLower() == normalizedStatusLower);
                    }

                    plaidWebhooks = await plaidWebhookQuery
                        .OrderByDescending(x => x.ProcessedAtUtc)
                        .Take(normalizedTake)
                        .Select(static webhook => new ProviderTimelineEventResponse(
                            "PlaidWebhook",
                            string.IsNullOrWhiteSpace(webhook.WebhookCode)
                                ? webhook.WebhookType
                                : $"{webhook.WebhookType}.{webhook.WebhookCode}",
                            webhook.ProcessingStatus,
                            webhook.ProcessedAtUtc,
                            string.IsNullOrWhiteSpace(webhook.WebhookCode)
                                ? $"Plaid webhook {webhook.WebhookType} -> {webhook.ProcessingStatus}."
                                : $"Plaid webhook {webhook.WebhookType}.{webhook.WebhookCode} -> {webhook.ProcessingStatus}.",
                            webhook.ErrorMessage,
                            null,
                            null))
                        .ToArrayAsync(cancellationToken);
                }

                ProviderTimelineEventResponse[] notifications = [];
                if (includeNotifications)
                {
                    var notificationQuery = dbContext.SpendNotificationEvents
                        .AsNoTracking()
                        .Where(x => x.FamilyId == familyId);

                    if (!string.IsNullOrWhiteSpace(normalizedStatus))
                    {
                        notificationQuery = notificationQuery
                            .Where(x => x.Status != null && x.Status.ToLower() == normalizedStatusLower);
                    }

                    notifications = await notificationQuery
                        .OrderByDescending(x => x.LastAttemptAtUtc ?? x.CreatedAtUtc)
                        .Take(normalizedTake)
                        .Select(static notification => new ProviderTimelineEventResponse(
                            "NotificationDispatch",
                            notification.Channel,
                            notification.Status,
                            notification.LastAttemptAtUtc ?? notification.CreatedAtUtc,
                            $"Spend notification via {notification.Channel} -> {notification.Status}.",
                            notification.ErrorMessage,
                            null,
                            notification.Id))
                        .ToArrayAsync(cancellationToken);
                }

                var timeline = webhooks
                    .Concat(plaidWebhooks)
                    .Concat(notifications)
                    .OrderByDescending(static item => item.OccurredAtUtc)
                    .Select(item => item with { Detail = TrimActivityError(item.Detail) })
                    .Take(normalizedTake)
                    .ToArray();

                return Results.Ok(new ProviderActivityTimelineResponse(
                    FamilyId: familyId,
                    GeneratedAtUtc: DateTimeOffset.UtcNow,
                    RequestedTake: normalizedTake,
                    Events: timeline,
                    TraceId: httpContext.TraceIdentifier));
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetProviderActivityTimeline")
            .WithOpenApi();
    }
}
