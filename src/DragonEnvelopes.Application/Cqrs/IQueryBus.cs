namespace DragonEnvelopes.Application.Cqrs;

public interface IQueryBus
{
    Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
}
