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

                var webhooks = await dbContext.StripeWebhookEvents
                    .AsNoTracking()
                    .Where(x => x.FamilyId == familyId)
                    .OrderByDescending(x => x.ProcessedAtUtc)
                    .Take(normalizedTake)
                    .Select(static webhook => new ProviderTimelineEventResponse(
                        "StripeWebhook",
                        webhook.EventType,
                        webhook.ProcessingStatus,
                        webhook.ProcessedAtUtc,
                        $"Stripe webhook {webhook.EventType} -> {webhook.ProcessingStatus}.",
                        webhook.ErrorMessage,
                        null))
                    .ToArrayAsync(cancellationToken);

                var notifications = await dbContext.SpendNotificationEvents
                    .AsNoTracking()
                    .Where(x => x.FamilyId == familyId)
                    .OrderByDescending(x => x.LastAttemptAtUtc ?? x.CreatedAtUtc)
                    .Take(normalizedTake)
                    .Select(static notification => new ProviderTimelineEventResponse(
                        "NotificationDispatch",
                        notification.Channel,
                        notification.Status,
                        notification.LastAttemptAtUtc ?? notification.CreatedAtUtc,
                        $"Spend notification via {notification.Channel} -> {notification.Status}.",
                        notification.ErrorMessage,
                        notification.Id))
                    .ToArrayAsync(cancellationToken);

                var timeline = webhooks
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
