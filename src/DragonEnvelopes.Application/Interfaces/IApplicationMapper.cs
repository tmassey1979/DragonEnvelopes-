namespace DragonEnvelopes.Application.Interfaces;

public interface IApplicationMapper
{
    T Map<T>(object source) where T : class;
}

