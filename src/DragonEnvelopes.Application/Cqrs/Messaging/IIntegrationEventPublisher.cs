namespace DragonEnvelopes.Application.Cqrs.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<TIntegrationEvent>(
        string routingKey,
        TIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default);
}
