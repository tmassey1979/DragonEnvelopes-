using DragonEnvelopes.Application.Cqrs.Messaging;
using Microsoft.Extensions.Logging;

namespace DragonEnvelopes.Infrastructure.Messaging;

public sealed class NoOpIntegrationOutboxMessagePublisher(
    ILogger<NoOpIntegrationOutboxMessagePublisher> logger) : IIntegrationOutboxMessagePublisher
{
    public Task PublishAsync(
        IntegrationOutboxEnvelopeMessage message,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Outbox message publisher is disabled. EventName={EventName}, RoutingKey={RoutingKey}",
            message.EventName,
            message.RoutingKey);
        return Task.CompletedTask;
    }
}
