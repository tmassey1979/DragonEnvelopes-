using DragonEnvelopes.Application.Cqrs;
using Microsoft.Extensions.DependencyInjection;

namespace DragonEnvelopes.Infrastructure.Cqrs;

public sealed class InProcessQueryBus(IServiceProvider serviceProvider) : IQueryBus
{
    public Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var queryType = query.GetType();
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResult));
        var handler = serviceProvider.GetRequiredService(handlerType);
        var handleMethod = handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResult>, TResult>.HandleAsync))
            ?? throw new InvalidOperationException($"Query handler {handlerType.Name} is missing HandleAsync.");
        var invocationResult = handleMethod.Invoke(handler, [query, cancellationToken]);

        if (invocationResult is not Task<TResult> resultTask)
        {
            throw new InvalidOperationException(
                $"Query handler {handlerType.Name} returned an invalid result type for query {queryType.Name}.");
        }

        return resultTask;
    }
}
