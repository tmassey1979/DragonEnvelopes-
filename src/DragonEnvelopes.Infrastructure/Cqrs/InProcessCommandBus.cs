using DragonEnvelopes.Application.Cqrs;
using Microsoft.Extensions.DependencyInjection;

namespace DragonEnvelopes.Infrastructure.Cqrs;

public sealed class InProcessCommandBus(IServiceProvider serviceProvider) : ICommandBus
{
    public Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResult));
        var handler = serviceProvider.GetRequiredService(handlerType);
        var handleMethod = handlerType.GetMethod(nameof(ICommandHandler<ICommand<TResult>, TResult>.HandleAsync))
            ?? throw new InvalidOperationException($"Command handler {handlerType.Name} is missing HandleAsync.");
        var invocationResult = handleMethod.Invoke(handler, [command, cancellationToken]);

        if (invocationResult is not Task<TResult> resultTask)
        {
            throw new InvalidOperationException(
                $"Command handler {handlerType.Name} returned an invalid result type for command {commandType.Name}.");
        }

        return resultTask;
    }
}
