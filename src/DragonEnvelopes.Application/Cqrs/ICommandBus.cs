namespace DragonEnvelopes.Application.Cqrs;

public interface ICommandBus
{
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
}
