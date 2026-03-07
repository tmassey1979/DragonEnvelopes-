using System.Diagnostics;
using System.Text.Json;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Services;

internal static class IntegrationOutboxEnqueuer
{
    private const string SchemaVersion = "1.0";

    public static async Task EnqueueAsync<TPayload>(
        IIntegrationOutboxRepository? outboxRepository,
        Guid? familyId,
        string sourceService,
        string routingKey,
        string eventName,
        TPayload payload,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken)
    {
        if (outboxRepository is null)
        {
            return;
        }

        var outboxMessage = new IntegrationOutboxMessage(
            Guid.NewGuid(),
            familyId,
            ResolveEventId(payload),
            routingKey,
            eventName,
            SchemaVersion,
            sourceService,
            ResolveCorrelationId(payload),
            causationId: null,
            JsonSerializer.Serialize(payload),
            ResolveOccurredAtUtc(payload),
            createdAtUtc);
        await outboxRepository.AddAsync(outboxMessage, cancellationToken);
    }

    private static string ResolveCorrelationId<TPayload>(TPayload payload)
    {
        if (TryGetStringProperty(payload, "CorrelationId", out var correlationId))
        {
            return correlationId;
        }

        return Activity.Current?.Id ?? Guid.NewGuid().ToString("D");
    }

    private static string ResolveEventId<TPayload>(TPayload payload)
    {
        if (TryGetGuidProperty(payload, "EventId", out var eventId))
        {
            return eventId.ToString("D");
        }

        return Guid.NewGuid().ToString("D");
    }

    private static DateTimeOffset ResolveOccurredAtUtc<TPayload>(TPayload payload)
    {
        if (TryGetDateTimeOffsetProperty(payload, "OccurredAtUtc", out var occurredAtUtc))
        {
            return occurredAtUtc;
        }

        return DateTimeOffset.UtcNow;
    }

    private static bool TryGetStringProperty<TPayload>(TPayload payload, string propertyName, out string value)
    {
        value = string.Empty;
        var property = payload?.GetType().GetProperty(propertyName);
        if (property?.GetValue(payload) is not string stringValue || string.IsNullOrWhiteSpace(stringValue))
        {
            return false;
        }

        value = stringValue.Trim();
        return true;
    }

    private static bool TryGetGuidProperty<TPayload>(TPayload payload, string propertyName, out Guid value)
    {
        value = Guid.Empty;
        var property = payload?.GetType().GetProperty(propertyName);
        if (property?.GetValue(payload) is not Guid guidValue || guidValue == Guid.Empty)
        {
            return false;
        }

        value = guidValue;
        return true;
    }

    private static bool TryGetDateTimeOffsetProperty<TPayload>(TPayload payload, string propertyName, out DateTimeOffset value)
    {
        value = default;
        var property = payload?.GetType().GetProperty(propertyName);
        if (property?.GetValue(payload) is not DateTimeOffset dateTimeOffsetValue || dateTimeOffsetValue == default)
        {
            return false;
        }

        value = dateTimeOffsetValue;
        return true;
    }
}
