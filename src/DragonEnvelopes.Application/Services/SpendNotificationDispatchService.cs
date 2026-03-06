using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using Microsoft.Extensions.Logging;

namespace DragonEnvelopes.Application.Services;

public sealed class SpendNotificationDispatchService(
    ISpendNotificationEventRepository spendNotificationEventRepository,
    IClock clock,
    ILogger<SpendNotificationDispatchService> logger) : ISpendNotificationDispatchService
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
                await DeliverAsync(notification, cancellationToken);
                notification.MarkSent(clock.UtcNow);
                sent += 1;
            }
            catch (Exception ex)
            {
                notification.MarkRetry(ex.Message, clock.UtcNow, MaxAttempts);
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

        if (!notification.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainValidationException("Only failed notification events can be retried manually.");
        }

        try
        {
            await DeliverAsync(notification, cancellationToken);
            notification.MarkSent(clock.UtcNow);
        }
        catch (Exception ex)
        {
            notification.MarkRetry(ex.Message, clock.UtcNow, MaxAttempts + 1);
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
}
