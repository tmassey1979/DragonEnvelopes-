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
}
