using System.Text.Json;
using DragonEnvelopes.Application.Cqrs.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DragonEnvelopes.Financial.Api.Services;

public sealed class LedgerTransactionCreatedConsumer(
    IOptions<RabbitMqMessagingOptions> optionsAccessor,
    ILogger<LedgerTransactionCreatedConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqMessagingOptions _options = optionsAccessor.Value;
    private IConnection? _connection;
    private IModel? _channel;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || !_options.EnableLedgerTransactionConsumer)
        {
            logger.LogInformation("Ledger transaction RabbitMQ consumer is disabled by configuration.");
            return Task.CompletedTask;
        }

        return ConsumeWithRetryAsync(stoppingToken);
    }

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<LedgerTransactionCreatedIntegrationEvent>(
                args.Body.Span,
                SerializerOptions);
            if (payload is null)
            {
                logger.LogWarning("Received empty or invalid ledger transaction event payload.");
                _channel.BasicAck(args.DeliveryTag, multiple: false);
                return;
            }

            logger.LogInformation(
                "Consumed ledger transaction event. EventId={EventId}, FamilyId={FamilyId}, TransactionId={TransactionId}, Amount={Amount}",
                payload.EventId,
                payload.FamilyId,
                payload.TransactionId,
                payload.Amount);

            _channel.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process ledger transaction event; message nacked.");
            _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
        }

        await Task.CompletedTask;
    }

    public override void Dispose()
    {
        DisposeConnectionArtifacts();
        base.Dispose();
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

    private static async Task WaitUntilCancelledAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private async Task ConsumeWithRetryAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                InitializeConsumer();
                logger.LogInformation(
                    "Started ledger transaction consumer. Queue={Queue}, Exchange={Exchange}, RoutingKey={RoutingKey}",
                    _options.LedgerTransactionCreatedQueue,
                    _options.ExchangeName,
                    IntegrationEventRoutingKeys.LedgerTransactionCreatedV1);

                await WaitUntilCancelledAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Ledger transaction consumer start failed. Retrying in 5 seconds.");
                DisposeConnectionArtifacts();
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private void InitializeConsumer()
    {
        DisposeConnectionArtifacts();

        var factory = BuildConnectionFactory(_options);
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(
            exchange: _options.ExchangeName,
            type: _options.ExchangeType,
            durable: _options.DurableExchange,
            autoDelete: false,
            arguments: null);
        _channel.QueueDeclare(
            queue: _options.LedgerTransactionCreatedQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        _channel.QueueBind(
            queue: _options.LedgerTransactionCreatedQueue,
            exchange: _options.ExchangeName,
            routingKey: IntegrationEventRoutingKeys.LedgerTransactionCreatedV1);
        _channel.BasicQos(prefetchSize: 0, prefetchCount: _options.ConsumerPrefetchCount, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += HandleMessageAsync;

        _channel.BasicConsume(
            queue: _options.LedgerTransactionCreatedQueue,
            autoAck: false,
            consumer: consumer);
    }

    private void DisposeConnectionArtifacts()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        _channel = null;
        _connection = null;
    }
}
