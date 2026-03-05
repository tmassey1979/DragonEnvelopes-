using DragonEnvelopes.Application.Interfaces;

namespace DragonEnvelopes.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

