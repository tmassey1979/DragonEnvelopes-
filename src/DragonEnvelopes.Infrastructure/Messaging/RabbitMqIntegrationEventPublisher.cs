using System.Text.Json;
using DragonEnvelopes.Application.Cqrs.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DragonEnvelopes.Infrastructure.Messaging;

public sealed class RabbitMqIntegrationEventPublisher(
    IOptions<RabbitMqMessagingOptions> optionsAccessor,
    ILogger<RabbitMqIntegrationEventPublisher> logger) : IIntegrationEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
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

            var payload = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, SerializerOptions);
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Type = typeof(TIntegrationEvent).Name;
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

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
                typeof(TIntegrationEvent).Name);
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
}
