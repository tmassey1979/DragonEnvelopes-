using System.Text.Json;
using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Contracts.IntegrationEvents;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DragonEnvelopes.Infrastructure.Messaging;

public sealed class RabbitMqIntegrationOutboxMessagePublisher(
    IOptions<RabbitMqMessagingOptions> optionsAccessor) : IIntegrationOutboxMessagePublisher
{
    private readonly RabbitMqMessagingOptions _options = optionsAccessor.Value;

    public Task PublishAsync(
        IntegrationOutboxEnvelopeMessage message,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("RabbitMQ outbox publishing is disabled.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(
            exchange: _options.ExchangeName,
            type: _options.ExchangeType,
            durable: _options.DurableExchange,
            autoDelete: false,
            arguments: null);

        var payload = JsonSerializer.Deserialize<JsonElement>(message.PayloadJson);
        var envelope = new IntegrationEventEnvelope<JsonElement>(
            message.EventId,
            message.EventName,
            message.SchemaVersion,
            message.OccurredAtUtc,
            DateTimeOffset.UtcNow,
            message.SourceService,
            message.CorrelationId,
            message.CausationId,
            message.FamilyId,
            payload);
        var serialized = IntegrationEventEnvelopeJson.SerializeToUtf8Bytes(envelope);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Type = envelope.EventName;
        properties.CorrelationId = envelope.CorrelationId;
        properties.MessageId = envelope.EventId;
        properties.Timestamp = new AmqpTimestamp(envelope.PublishedAtUtc.ToUnixTimeSeconds());

        channel.BasicPublish(
            exchange: _options.ExchangeName,
            routingKey: message.RoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: serialized);

        return Task.CompletedTask;
    }
}
