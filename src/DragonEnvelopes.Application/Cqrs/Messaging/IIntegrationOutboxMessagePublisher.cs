namespace DragonEnvelopes.Application.Cqrs.Messaging;

public interface IIntegrationOutboxMessagePublisher
{
    Task PublishAsync(
        IntegrationOutboxEnvelopeMessage message,
        CancellationToken cancellationToken = default);
}
