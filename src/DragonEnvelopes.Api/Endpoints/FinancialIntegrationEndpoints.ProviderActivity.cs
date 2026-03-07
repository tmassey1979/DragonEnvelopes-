using System.Security.Claims;
using System.Text;
using System.Text.Json;
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
                            null,
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
                            webhook.Id,
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

        v1.MapGet("/families/{familyId:guid}/financial/provider-activity/timeline/events/{source}/{eventId:guid}", async (
                Guid familyId,
                string source,
                Guid eventId,
                ClaimsPrincipal user,
                DragonEnvelopesDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                if (!await EndpointAccessGuards.UserHasFamilyAccessAsync(user, familyId, dbContext, cancellationToken))
                {
                    return Results.Forbid();
                }

                var normalizedSource = string.IsNullOrWhiteSpace(source)
                    ? string.Empty
                    : source.Trim();
                if (normalizedSource.Equals("StripeWebhook", StringComparison.OrdinalIgnoreCase))
                {
                    var webhook = await dbContext.StripeWebhookEvents
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == eventId && x.FamilyId == familyId, cancellationToken);
                    if (webhook is null)
                    {
                        return Results.NotFound();
                    }

                    var payloadPreview = BuildPayloadPreview(webhook.PayloadJson, out var isPayloadTruncated);
                    return Results.Ok(new ProviderTimelineEventDetailResponse(
                        FamilyId: familyId,
                        Source: "StripeWebhook",
                        EventId: webhook.Id,
                        EventType: webhook.EventType,
                        Status: webhook.ProcessingStatus,
                        OccurredAtUtc: webhook.ProcessedAtUtc,
                        Summary: $"Stripe webhook {webhook.EventType} -> {webhook.ProcessingStatus}.",
                        Detail: TrimActivityError(webhook.ErrorMessage),
                        PayloadPreviewJson: payloadPreview,
                        PayloadTruncated: isPayloadTruncated));
                }

                if (normalizedSource.Equals("PlaidWebhook", StringComparison.OrdinalIgnoreCase))
                {
                    var webhook = await dbContext.PlaidWebhookEvents
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == eventId && x.FamilyId == familyId, cancellationToken);
                    if (webhook is null)
                    {
                        return Results.NotFound();
                    }

                    var eventType = string.IsNullOrWhiteSpace(webhook.WebhookCode)
                        ? webhook.WebhookType
                        : $"{webhook.WebhookType}.{webhook.WebhookCode}";
                    var payloadPreview = BuildPayloadPreview(webhook.PayloadJson, out var isPayloadTruncated);
                    return Results.Ok(new ProviderTimelineEventDetailResponse(
                        FamilyId: familyId,
                        Source: "PlaidWebhook",
                        EventId: webhook.Id,
                        EventType: eventType,
                        Status: webhook.ProcessingStatus,
                        OccurredAtUtc: webhook.ProcessedAtUtc,
                        Summary: $"Plaid webhook {eventType} -> {webhook.ProcessingStatus}.",
                        Detail: TrimActivityError(webhook.ErrorMessage),
                        PayloadPreviewJson: payloadPreview,
                        PayloadTruncated: isPayloadTruncated));
                }

                if (normalizedSource.Equals("NotificationDispatch", StringComparison.OrdinalIgnoreCase))
                {
                    var notification = await dbContext.SpendNotificationEvents
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == eventId && x.FamilyId == familyId, cancellationToken);
                    if (notification is null)
                    {
                        return Results.NotFound();
                    }

                    return Results.Ok(new ProviderTimelineEventDetailResponse(
                        FamilyId: familyId,
                        Source: "NotificationDispatch",
                        EventId: notification.Id,
                        EventType: notification.Channel,
                        Status: notification.Status,
                        OccurredAtUtc: notification.LastAttemptAtUtc ?? notification.CreatedAtUtc,
                        Summary: $"Spend notification via {notification.Channel} -> {notification.Status}.",
                        Detail: TrimActivityError(notification.ErrorMessage),
                        PayloadPreviewJson: null,
                        PayloadTruncated: false));
                }

                return Results.BadRequest("source must be StripeWebhook, PlaidWebhook, or NotificationDispatch.");
            })
            .RequireAuthorization(ApiAuthorizationPolicies.AnyFamilyMember)
            .WithName("GetProviderActivityTimelineEventDetail")
            .WithOpenApi();
    }

    private static readonly string[] SensitivePayloadKeyFragments =
    [
        "secret",
        "token",
        "password",
        "authorization",
        "api_key",
        "account_number",
        "routing_number",
        "card_number",
        "pan",
        "cvv",
        "cvc",
        "ssn",
        "email",
        "phone"
    ];

    private static string? BuildPayloadPreview(string? payloadJson, out bool isTruncated)
    {
        isTruncated = false;
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        string redacted;
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                WriteRedactedJson(document.RootElement, writer, propertyName: null);
            }

            redacted = Encoding.UTF8.GetString(buffer.ToArray());
        }
        catch (JsonException)
        {
            redacted = payloadJson.Trim();
        }

        const int maxLength = 4096;
        if (redacted.Length <= maxLength)
        {
            return redacted;
        }

        isTruncated = true;
        return $"{redacted[..maxLength]}...";
    }

    private static void WriteRedactedJson(JsonElement element, Utf8JsonWriter writer, string? propertyName)
    {
        if (IsSensitivePayloadKey(propertyName))
        {
            writer.WriteStringValue("***redacted***");
            return;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    WriteRedactedJson(property.Value, writer, property.Name);
                }

                writer.WriteEndObject();
                return;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteRedactedJson(item, writer, propertyName);
                }

                writer.WriteEndArray();
                return;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                return;
            default:
                element.WriteTo(writer);
                return;
        }
    }

    private static bool IsSensitivePayloadKey(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return false;
        }

        var normalized = propertyName.Trim().ToLowerInvariant();
        return SensitivePayloadKeyFragments.Any(fragment => normalized.Contains(fragment, StringComparison.Ordinal));
    }
}
