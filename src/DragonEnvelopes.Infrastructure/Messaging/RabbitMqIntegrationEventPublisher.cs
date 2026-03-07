using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Contracts.IntegrationEvents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DragonEnvelopes.Infrastructure.Messaging;

public sealed class RabbitMqIntegrationEventPublisher(
    IOptions<RabbitMqMessagingOptions> optionsAccessor,
    ILogger<RabbitMqIntegrationEventPublisher> logger) : IIntegrationEventPublisher
{
    private readonly RabbitMqMessagingOptions _options = optionsAccessor.Value;

    public Task PublishAsync<TIntegrationEvent>(
        string routingKey,
        TIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(routingKey))
        {
            throw new InvalidOperationException("RabbitMQ routing key is required.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var factory = BuildConnectionFactory(_options);
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(
                exchange: _options.ExchangeName,
                type: _options.ExchangeType,
                durable: _options.DurableExchange,
                autoDelete: false,
                arguments: null);

            var publishedAtUtc = DateTimeOffset.UtcNow;
            var eventId = ResolveEventId(integrationEvent);
            var occurredAtUtc = ResolveOccurredAtUtc(integrationEvent, publishedAtUtc);
            var familyId = ResolveFamilyId(integrationEvent);
            var eventName = ResolveEventName(routingKey, integrationEvent);
            var schemaVersion = ResolveSchemaVersion(routingKey);
            var sourceService = string.IsNullOrWhiteSpace(_options.SourceService)
                ? "dragonenvelopes-service"
                : _options.SourceService.Trim();
            var correlationId = System.Diagnostics.Activity.Current?.Id ?? eventId;

            var envelope = IntegrationEventEnvelopeFactory.Create(
                eventName,
                schemaVersion,
                sourceService,
                correlationId,
                causationId: null,
                familyId,
                integrationEvent,
                occurredAtUtc,
                eventId);

            var payload = IntegrationEventEnvelopeJson.SerializeToUtf8Bytes(envelope);
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Type = envelope.EventName;
            properties.CorrelationId = envelope.CorrelationId;
            properties.MessageId = envelope.EventId;
            properties.Timestamp = new AmqpTimestamp(publishedAtUtc.ToUnixTimeSeconds());

            channel.BasicPublish(
                exchange: _options.ExchangeName,
                routingKey: routingKey.Trim(),
                mandatory: false,
                basicProperties: properties,
                body: payload);

            logger.LogInformation(
                "Published integration event. Exchange={Exchange}, RoutingKey={RoutingKey}, EventType={EventType}",
                _options.ExchangeName,
                routingKey,
                envelope.EventName);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to publish integration event. Exchange={Exchange}, RoutingKey={RoutingKey}, EventType={EventType}",
                _options.ExchangeName,
                routingKey,
                typeof(TIntegrationEvent).Name);
        }

        return Task.CompletedTask;
    }

    private static ConnectionFactory BuildConnectionFactory(RabbitMqMessagingOptions options)
    {
        return new ConnectionFactory
        {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true
        };
    }

    private static string ResolveEventId<TIntegrationEvent>(TIntegrationEvent integrationEvent)
    {
        if (TryGetValue(integrationEvent, "EventId", out Guid guidValue))
        {
            return guidValue.ToString("D");
        }

        if (TryGetValue(integrationEvent, "EventId", out string? stringValue)
            && !string.IsNullOrWhiteSpace(stringValue))
        {
            return stringValue.Trim();
        }

        return Guid.NewGuid().ToString("D");
    }

    private static DateTimeOffset ResolveOccurredAtUtc<TIntegrationEvent>(TIntegrationEvent integrationEvent, DateTimeOffset fallback)
    {
        if (TryGetValue(integrationEvent, "OccurredAtUtc", out DateTimeOffset occurredAtUtcValue)
            && occurredAtUtcValue != default)
        {
            return occurredAtUtcValue;
        }

        if (TryGetValue(integrationEvent, "OccurredAtUtc", out DateTime occurredAtValue)
            && occurredAtValue != default)
        {
            return occurredAtValue.Kind == DateTimeKind.Utc
                ? new DateTimeOffset(occurredAtValue)
                : new DateTimeOffset(occurredAtValue.ToUniversalTime());
        }

        return fallback;
    }

    private static Guid? ResolveFamilyId<TIntegrationEvent>(TIntegrationEvent integrationEvent)
    {
        if (TryGetValue(integrationEvent, "FamilyId", out Guid familyIdValue)
            && familyIdValue != Guid.Empty)
        {
            return familyIdValue;
        }

        if (TryGetValue(integrationEvent, "FamilyId", out Guid? nullableFamilyId)
            && nullableFamilyId.HasValue
            && nullableFamilyId.Value != Guid.Empty)
        {
            return nullableFamilyId.Value;
        }

        return null;
    }

    private static string ResolveEventName<TIntegrationEvent>(string routingKey, TIntegrationEvent integrationEvent)
    {
        if (!string.IsNullOrWhiteSpace(routingKey))
        {
            return routingKey.Trim();
        }

        return typeof(TIntegrationEvent).Name;
    }

    private static string ResolveSchemaVersion(string routingKey)
    {
        if (string.IsNullOrWhiteSpace(routingKey))
        {
            return "1.0";
        }

        var segments = routingKey.Trim().Split('.', StringSplitOptions.RemoveEmptyEntries);
        var last = segments.LastOrDefault();
        if (string.IsNullOrWhiteSpace(last)
            || !last.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            return "1.0";
        }

        if (int.TryParse(last[1..], out var majorVersion) && majorVersion > 0)
        {
            return $"{majorVersion}.0";
        }

        return "1.0";
    }

    private static bool TryGetValue<TSource, TValue>(TSource source, string propertyName, out TValue value)
    {
        value = default!;
        if (source is null)
        {
            return false;
        }

        var property = source.GetType().GetProperty(propertyName);
        if (property is null)
        {
            return false;
        }

        var rawValue = property.GetValue(source);
        if (rawValue is not TValue typedValue)
        {
            return false;
        }

        value = typedValue;
        return true;
    }
}
