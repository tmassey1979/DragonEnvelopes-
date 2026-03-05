using DragonEnvelopes.Application.Interfaces;

namespace DragonEnvelopes.Application.Mapping;

public sealed class IdentityMapper : IApplicationMapper
{
    public T Map<T>(object source) where T : class
    {
        if (source is T typed)
        {
            return typed;
        }

        throw new InvalidOperationException($"Cannot map {source.GetType().Name} to {typeof(T).Name}.");
    }
}

