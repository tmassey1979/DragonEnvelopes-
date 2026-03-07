namespace DragonEnvelopes.Application.Cqrs.Messaging;

public sealed class RabbitMqMessagingOptions
{
    public bool Enabled { get; init; }

    public string HostName { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public string UserName { get; init; } = "guest";

    public string Password { get; init; } = "guest";

    public string VirtualHost { get; init; } = "/";

    public string ExchangeName { get; init; } = "dragonenvelopes.events";

    public string ExchangeType { get; init; } = "topic";

    public bool DurableExchange { get; init; } = true;

    public string SourceService { get; init; } = "dragonenvelopes-service";

    public bool EnableLedgerTransactionConsumer { get; init; } = true;

    public string LedgerTransactionCreatedQueue { get; init; } = "dragonenvelopes.financial.ledger-transaction-created";

    public string LedgerTransactionCreatedRetryQueue { get; init; } = "dragonenvelopes.financial.ledger-transaction-created.retry";

    public string LedgerTransactionCreatedRetryRoutingKey { get; init; } = "ledger.transaction.created.retry.v1";

    public string LedgerTransactionCreatedDeadLetterQueue { get; init; } = "dragonenvelopes.financial.ledger-transaction-created.dlq";

    public string LedgerTransactionCreatedDeadLetterRoutingKey { get; init; } = "ledger.transaction.created.dlq.v1";

    public int ConsumerMaxRetryAttempts { get; init; } = 5;

    public int ConsumerRetryDelayMilliseconds { get; init; } = 30000;

    public ushort ConsumerPrefetchCount { get; init; } = 20;
}
