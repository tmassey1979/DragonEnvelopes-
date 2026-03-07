using DragonEnvelopes.Application.Cqrs.Messaging;
using Microsoft.Extensions.Logging;

namespace DragonEnvelopes.Infrastructure.Messaging;

public sealed class NoOpIntegrationEventPublisher(
    ILogger<NoOpIntegrationEventPublisher> logger) : IIntegrationEventPublisher
{
    public Task PublishAsync<TIntegrationEvent>(
        string routingKey,
        TIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Integration event publishing is disabled. RoutingKey={RoutingKey}, EventType={EventType}",
            routingKey,
            typeof(TIntegrationEvent).Name);
        return Task.CompletedTask;
    }
}
