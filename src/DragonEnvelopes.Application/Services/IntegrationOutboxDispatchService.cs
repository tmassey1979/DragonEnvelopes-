using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DragonEnvelopes.Application.Services;

public sealed class IntegrationOutboxDispatchService(
    IIntegrationOutboxRepository integrationOutboxRepository,
    IIntegrationOutboxMessagePublisher outboxMessagePublisher,
    IClock clock,
    ILogger<IntegrationOutboxDispatchService> logger) : IIntegrationOutboxDispatchService
{
    public async Task<IntegrationOutboxDispatchResult> DispatchPendingAsync(
        string sourceService,
        int take,
        CancellationToken cancellationToken = default)
    {
        var normalizedSourceService = NormalizeSourceService(sourceService);
        var now = clock.UtcNow;
        var batch = await integrationOutboxRepository.ListDispatchableAsync(
            now,
            Math.Clamp(take, 1, 500),
            normalizedSourceService,
            cancellationToken);
        if (batch.Count == 0)
        {
            var pendingWithoutDispatch = await integrationOutboxRepository.CountPendingAsync(
                normalizedSourceService,
                cancellationToken);
            return new IntegrationOutboxDispatchResult(
                LoadedCount: 0,
                PublishedCount: 0,
                FailedCount: 0,
                PendingCount: pendingWithoutDispatch);
        }

        var publishedCount = 0;
        var failedCount = 0;
        foreach (var message in batch)
        {
            try
            {
                await outboxMessagePublisher.PublishAsync(
                    new IntegrationOutboxEnvelopeMessage(
                        message.RoutingKey,
                        message.EventId,
                        message.EventName,
                        message.SchemaVersion,
                        message.OccurredAtUtc,
                        message.SourceService,
                        message.CorrelationId,
                        message.CausationId,
                        message.FamilyId,
                        message.PayloadJson),
                    cancellationToken);
                message.MarkDispatched(clock.UtcNow);
                publishedCount += 1;
            }
            catch (Exception ex)
            {
                var attemptedAtUtc = clock.UtcNow;
                message.MarkRetry(
                    TruncateError(ex.Message),
                    attemptedAtUtc,
                    ComputeRetryDelay(message.AttemptCount + 1));
                failedCount += 1;
                logger.LogWarning(
                    ex,
                    "Outbox publish failed and will be retried. OutboxMessageId={OutboxMessageId}, EventName={EventName}, AttemptCount={AttemptCount}",
                    message.Id,
                    message.EventName,
                    message.AttemptCount);
            }
        }

        await integrationOutboxRepository.SaveChangesAsync(cancellationToken);
        var pendingCount = await integrationOutboxRepository.CountPendingAsync(
            normalizedSourceService,
            cancellationToken);

        return new IntegrationOutboxDispatchResult(
            LoadedCount: batch.Count,
            PublishedCount: publishedCount,
            FailedCount: failedCount,
            PendingCount: pendingCount);
    }

    private static TimeSpan ComputeRetryDelay(int attemptNumber)
    {
        var normalizedAttempt = Math.Max(1, attemptNumber);
        var backoffExponent = Math.Min(6, normalizedAttempt - 1);
        var delaySeconds = 5 * Math.Pow(2, backoffExponent);
        return TimeSpan.FromSeconds(delaySeconds);
    }

    private static string TruncateError(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return "Unknown publish failure.";
        }

        var trimmed = errorMessage.Trim();
        return trimmed.Length <= 1900
            ? trimmed
            : trimmed[..1900];
    }

    private static string NormalizeSourceService(string sourceService)
    {
        if (string.IsNullOrWhiteSpace(sourceService))
        {
            throw new InvalidOperationException("Outbox source service is required.");
        }

        return sourceService.Trim();
    }
}
