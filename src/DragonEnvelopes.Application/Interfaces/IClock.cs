namespace DragonEnvelopes.Application.Interfaces;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}


