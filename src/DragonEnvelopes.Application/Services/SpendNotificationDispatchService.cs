using System.Diagnostics;
using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using Microsoft.Extensions.Logging;

namespace DragonEnvelopes.Application.Services;

public sealed class SpendNotificationDispatchService(
    ISpendNotificationEventRepository spendNotificationEventRepository,
    IClock clock,
    ILogger<SpendNotificationDispatchService> logger,
    IIntegrationOutboxRepository? integrationOutboxRepository = null) : ISpendNotificationDispatchService
{
    private const int MaxAttempts = 3;
    private const int BatchSize = 100;

    public async Task<SpendNotificationDispatchResult> DispatchPendingAsync(CancellationToken cancellationToken = default)
    {
        var pending = await spendNotificationEventRepository.ListDispatchableAsync(
            MaxAttempts,
            BatchSize,
            cancellationToken);
        if (pending.Count == 0)
        {
            return new SpendNotificationDispatchResult(0, 0);
        }

        var sent = 0;
        var failed = 0;
        foreach (var notification in pending)
        {
            try
            {
                var priorAttemptCount = notification.AttemptCount;
                await DeliverAsync(notification, cancellationToken);
                var sentAtUtc = clock.UtcNow;
                notification.MarkSent(sentAtUtc);
                if (priorAttemptCount > 0)
                {
                    await EnqueueFinancialOutboxAsync(
                        notification.FamilyId,
                        IntegrationEventRoutingKeys.FinancialProviderNotificationDispatchRetriedV1,
                        FinancialIntegrationEventNames.ProviderNotificationDispatchRetried,
                        new ProviderNotificationDispatchRetriedIntegrationEvent(
                            Guid.NewGuid(),
                            sentAtUtc,
                            notification.FamilyId,
                            ResolveCorrelationId(),
                            notification.Id,
                            notification.UserId,
                            notification.Channel,
                            notification.Amount,
                            notification.Merchant,
                            notification.AttemptCount,
                            notification.Status),
                        sentAtUtc,
                        cancellationToken);
                }
                sent += 1;
            }
            catch (Exception ex)
            {
                var attemptedAtUtc = clock.UtcNow;
                notification.MarkRetry(ex.Message, attemptedAtUtc, MaxAttempts);
                if (notification.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
                {
                    await EnqueueFinancialOutboxAsync(
                        notification.FamilyId,
                        IntegrationEventRoutingKeys.FinancialProviderNotificationDispatchFailedV1,
                        FinancialIntegrationEventNames.ProviderNotificationDispatchFailed,
                        new ProviderNotificationDispatchFailedIntegrationEvent(
                            Guid.NewGuid(),
                            attemptedAtUtc,
                            notification.FamilyId,
                            ResolveCorrelationId(),
                            notification.Id,
                            notification.UserId,
                            notification.Channel,
                            notification.Amount,
                            notification.Merchant,
                            notification.AttemptCount,
                            ex.Message),
                        attemptedAtUtc,
                        cancellationToken);
                }
                failed += 1;
                logger.LogWarning(
                    ex,
                    "Notification dispatch failed. NotificationId={NotificationId}, Channel={Channel}, Attempt={Attempt}",
                    notification.Id,
                    notification.Channel,
                    notification.AttemptCount);
            }
        }

        await spendNotificationEventRepository.SaveChangesAsync(cancellationToken);
        return new SpendNotificationDispatchResult(sent, failed);
    }

    public async Task<IReadOnlyList<SpendNotificationDispatchEventDetails>> ListFailedEventsAsync(
        Guid familyId,
        int take = 25,
        CancellationToken cancellationToken = default)
    {
        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        var failedEvents = await spendNotificationEventRepository.ListFailedByFamilyAsync(
            familyId,
            take,
            cancellationToken);

        return failedEvents.Select(Map).ToArray();
    }

    public async Task<SpendNotificationDispatchEventDetails> RetryFailedEventAsync(
        Guid familyId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return await ReplayEventCoreAsync(
            familyId,
            eventId,
            allowAlreadySent: false,
            cancellationToken);
    }

    public async Task<SpendNotificationDispatchEventDetails> ReplayEventAsync(
        Guid familyId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return await ReplayEventCoreAsync(
            familyId,
            eventId,
            allowAlreadySent: true,
            cancellationToken);
    }

    private async Task<SpendNotificationDispatchEventDetails> ReplayEventCoreAsync(
        Guid familyId,
        Guid eventId,
        bool allowAlreadySent,
        CancellationToken cancellationToken)
    {
        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (eventId == Guid.Empty)
        {
            throw new DomainValidationException("Notification event id is required.");
        }

        var notification = await spendNotificationEventRepository.GetByFamilyAndIdForUpdateAsync(
            familyId,
            eventId,
            cancellationToken);
        if (notification is null)
        {
            throw new DomainValidationException("Notification event was not found for this family.");
        }

        if (allowAlreadySent && notification.Status.Equals("Sent", StringComparison.OrdinalIgnoreCase))
        {
            // Idempotent replay behavior: already-sent events are returned as-is.
            return Map(notification);
        }

        if (!notification.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainValidationException("Only failed notification events can be retried manually.");
        }

        try
        {
            var priorAttemptCount = notification.AttemptCount;
            await DeliverAsync(notification, cancellationToken);
            var sentAtUtc = clock.UtcNow;
            notification.MarkSent(sentAtUtc);
            if (priorAttemptCount > 0)
            {
                await EnqueueFinancialOutboxAsync(
                    notification.FamilyId,
                    IntegrationEventRoutingKeys.FinancialProviderNotificationDispatchRetriedV1,
                    FinancialIntegrationEventNames.ProviderNotificationDispatchRetried,
                    new ProviderNotificationDispatchRetriedIntegrationEvent(
                        Guid.NewGuid(),
                        sentAtUtc,
                        notification.FamilyId,
                        ResolveCorrelationId(),
                        notification.Id,
                        notification.UserId,
                        notification.Channel,
                        notification.Amount,
                        notification.Merchant,
                        notification.AttemptCount,
                        notification.Status),
                    sentAtUtc,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            var attemptedAtUtc = clock.UtcNow;
            notification.MarkRetry(ex.Message, attemptedAtUtc, MaxAttempts + 1);
            if (notification.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
            {
                await EnqueueFinancialOutboxAsync(
                    notification.FamilyId,
                    IntegrationEventRoutingKeys.FinancialProviderNotificationDispatchFailedV1,
                    FinancialIntegrationEventNames.ProviderNotificationDispatchFailed,
                    new ProviderNotificationDispatchFailedIntegrationEvent(
                        Guid.NewGuid(),
                        attemptedAtUtc,
                        notification.FamilyId,
                        ResolveCorrelationId(),
                        notification.Id,
                        notification.UserId,
                        notification.Channel,
                        notification.Amount,
                        notification.Merchant,
                        notification.AttemptCount,
                        ex.Message),
                    attemptedAtUtc,
                    cancellationToken);
            }
            logger.LogWarning(
                ex,
                "Manual notification retry failed. NotificationId={NotificationId}, Channel={Channel}, Attempt={Attempt}",
                notification.Id,
                notification.Channel,
                notification.AttemptCount);
        }

        await spendNotificationEventRepository.SaveChangesAsync(cancellationToken);
        return Map(notification);
    }

    private Task DeliverAsync(DragonEnvelopes.Domain.Entities.SpendNotificationEvent notification, CancellationToken cancellationToken)
    {
        if (notification.Channel.Equals("Sms", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainValidationException("SMS provider is not configured.");
        }

        logger.LogInformation(
            "Dispatched spend notification. Channel={Channel}, UserId={UserId}, FamilyId={FamilyId}, EnvelopeId={EnvelopeId}, Amount={Amount}, Merchant={Merchant}, RemainingBalance={RemainingBalance}",
            notification.Channel,
            notification.UserId,
            notification.FamilyId,
            notification.EnvelopeId,
            notification.Amount,
            notification.Merchant,
            notification.RemainingBalance);

        return Task.CompletedTask;
    }

    private static SpendNotificationDispatchEventDetails Map(DragonEnvelopes.Domain.Entities.SpendNotificationEvent notification)
    {
        return new SpendNotificationDispatchEventDetails(
            notification.Id,
            notification.FamilyId,
            notification.UserId,
            notification.EnvelopeId,
            notification.CardId,
            notification.Channel,
            notification.Amount,
            notification.Merchant,
            notification.Status,
            notification.AttemptCount,
            notification.CreatedAtUtc,
            notification.LastAttemptAtUtc,
            notification.SentAtUtc,
            notification.ErrorMessage);
    }

    private static string ResolveCorrelationId()
    {
        return Activity.Current?.Id ?? Guid.NewGuid().ToString("D");
    }

    private Task EnqueueFinancialOutboxAsync<TPayload>(
        Guid familyId,
        string routingKey,
        string eventName,
        TPayload payload,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken)
    {
        return IntegrationOutboxEnqueuer.EnqueueAsync(
            integrationOutboxRepository,
            familyId,
            IntegrationEventSourceServices.FinancialApi,
            routingKey,
            eventName,
            payload,
            createdAtUtc,
            cancellationToken);
    }
}
