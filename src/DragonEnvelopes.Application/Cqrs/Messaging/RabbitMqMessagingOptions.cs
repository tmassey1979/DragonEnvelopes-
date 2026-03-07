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

    public bool EnableLedgerTransactionConsumer { get; init; } = true;

    public string LedgerTransactionCreatedQueue { get; init; } = "dragonenvelopes.financial.ledger-transaction-created";

    public ushort ConsumerPrefetchCount { get; init; } = 20;
}
