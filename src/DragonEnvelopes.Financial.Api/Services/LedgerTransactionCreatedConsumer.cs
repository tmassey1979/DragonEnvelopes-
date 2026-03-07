using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DragonEnvelopes.Financial.Api.Services;

public sealed class LedgerTransactionCreatedConsumer(
    IOptions<RabbitMqMessagingOptions> optionsAccessor,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<LedgerTransactionCreatedConsumer> logger) : BackgroundService
{
    private const string ConsumerName = "financial.ledger-transaction-created-consumer";
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
            LedgerTransactionCreatedMessageProcessResult result;
            using (var scope = serviceScopeFactory.CreateScope())
            {
                var processor = scope.ServiceProvider.GetRequiredService<LedgerTransactionCreatedMessageProcessor>();
                result = await processor.ProcessAsync(
                    args.Body.ToArray(),
                    args.RoutingKey,
                    _options.ConsumerMaxRetryAttempts);
            }

            switch (result.Disposition)
            {
                case ConsumerMessageDisposition.Ack:
                    _channel.BasicAck(args.DeliveryTag, multiple: false);
                    break;

                case ConsumerMessageDisposition.Retry:
                    PublishRetryMessage(args, result);
                    _channel.BasicAck(args.DeliveryTag, multiple: false);
                    break;

                case ConsumerMessageDisposition.DeadLetter:
                    await PublishDeadLetterMessageAsync(args, result);
                    _channel.BasicAck(args.DeliveryTag, multiple: false);
                    break;

                default:
                    _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process ledger transaction event; message nacked for requeue.");
            _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
        }
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
                    "Started ledger transaction consumer. Queue={Queue}, RetryQueue={RetryQueue}, DeadLetterQueue={DeadLetterQueue}, Exchange={Exchange}, RoutingKey={RoutingKey}",
                    _options.LedgerTransactionCreatedQueue,
                    _options.LedgerTransactionCreatedRetryQueue,
                    _options.LedgerTransactionCreatedDeadLetterQueue,
                    _options.ExchangeName,
                    IntegrationEventRoutingKeys.LedgerTransactionCreatedV1);
                LogQueueDepthSnapshot("consumer-start");

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

        _channel.QueueDeclare(
            queue: _options.LedgerTransactionCreatedRetryQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-message-ttl"] = _options.ConsumerRetryDelayMilliseconds,
                ["x-dead-letter-exchange"] = _options.ExchangeName,
                ["x-dead-letter-routing-key"] = IntegrationEventRoutingKeys.LedgerTransactionCreatedV1
            });
        _channel.QueueBind(
            queue: _options.LedgerTransactionCreatedRetryQueue,
            exchange: _options.ExchangeName,
            routingKey: _options.LedgerTransactionCreatedRetryRoutingKey);

        _channel.QueueDeclare(
            queue: _options.LedgerTransactionCreatedDeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        _channel.QueueBind(
            queue: _options.LedgerTransactionCreatedDeadLetterQueue,
            exchange: _options.ExchangeName,
            routingKey: _options.LedgerTransactionCreatedDeadLetterRoutingKey);

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

    private async Task PublishDeadLetterMessageAsync(
        BasicDeliverEventArgs args,
        LedgerTransactionCreatedMessageProcessResult result)
    {
        try
        {
            PublishMessage(
                args,
                _options.LedgerTransactionCreatedDeadLetterRoutingKey,
                result.AttemptCount,
                result.ErrorMessage,
                isDeadLetter: true);
            using var scope = serviceScopeFactory.CreateScope();
            var inboxRepository = scope.ServiceProvider.GetRequiredService<IIntegrationInboxRepository>();
            var deadLetteredCount = await inboxRepository.CountDeadLetteredAsync(ConsumerName);
            logger.LogWarning(
                "Ledger transaction event dead-lettered. IdempotencyKey={IdempotencyKey}, AttemptCount={AttemptCount}, DeadLetteredCount={DeadLetteredCount}",
                result.IdempotencyKey,
                result.AttemptCount,
                deadLetteredCount);
            LogQueueDepthSnapshot("dead-letter");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to publish dead-letter message. IdempotencyKey={IdempotencyKey}",
                result.IdempotencyKey);
            _channel?.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private void PublishRetryMessage(
        BasicDeliverEventArgs args,
        LedgerTransactionCreatedMessageProcessResult result)
    {
        try
        {
            PublishMessage(
                args,
                _options.LedgerTransactionCreatedRetryRoutingKey,
                result.AttemptCount,
                result.ErrorMessage,
                isDeadLetter: false);
            logger.LogInformation(
                "Requeued ledger transaction event for retry. IdempotencyKey={IdempotencyKey}, AttemptCount={AttemptCount}, RetryQueue={RetryQueue}",
                result.IdempotencyKey,
                result.AttemptCount,
                _options.LedgerTransactionCreatedRetryQueue);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to publish retry message. IdempotencyKey={IdempotencyKey}",
                result.IdempotencyKey);
            _channel?.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private void PublishMessage(
        BasicDeliverEventArgs args,
        string routingKey,
        int attemptCount,
        string? errorMessage,
        bool isDeadLetter)
    {
        if (_channel is null)
        {
            throw new InvalidOperationException("Consumer channel is not initialized.");
        }

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = args.BasicProperties?.ContentType ?? "application/json";
        properties.CorrelationId = args.BasicProperties?.CorrelationId;
        properties.MessageId = args.BasicProperties?.MessageId ?? Guid.NewGuid().ToString("D");
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.Headers = CloneHeaders(args.BasicProperties?.Headers);
        properties.Headers["x-dragonenvelopes-consumer"] = ConsumerName;
        properties.Headers["x-dragonenvelopes-attempt-count"] = (long)attemptCount;
        properties.Headers["x-dragonenvelopes-original-routing-key"] = args.RoutingKey;
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            properties.Headers["x-dragonenvelopes-last-error"] = errorMessage.Trim();
        }

        if (isDeadLetter)
        {
            properties.Headers["x-dragonenvelopes-dead-lettered"] = true;
            properties.Headers["x-dragonenvelopes-dead-lettered-at-utc"] = DateTimeOffset.UtcNow.ToString("O");
        }

        _channel.BasicPublish(
            exchange: _options.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: args.Body);
    }

    private void LogQueueDepthSnapshot(string trigger)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            var mainCount = _channel.MessageCount(_options.LedgerTransactionCreatedQueue);
            var retryCount = _channel.MessageCount(_options.LedgerTransactionCreatedRetryQueue);
            var deadLetterCount = _channel.MessageCount(_options.LedgerTransactionCreatedDeadLetterQueue);
            logger.LogInformation(
                "Ledger transaction queue depth snapshot. Trigger={Trigger}, Main={MainCount}, Retry={RetryCount}, DeadLetter={DeadLetterCount}",
                trigger,
                mainCount,
                retryCount,
                deadLetterCount);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Unable to collect queue depth snapshot for trigger {Trigger}.", trigger);
        }
    }

    private static Dictionary<string, object> CloneHeaders(IDictionary<string, object>? headers)
    {
        if (headers is null || headers.Count == 0)
        {
            return new Dictionary<string, object>(StringComparer.Ordinal);
        }

        var cloned = new Dictionary<string, object>(headers.Count, StringComparer.Ordinal);
        foreach (var header in headers)
        {
            if (header.Value is null)
            {
                continue;
            }

            cloned[header.Key] = header.Value;
        }

        return cloned;
    }
}
