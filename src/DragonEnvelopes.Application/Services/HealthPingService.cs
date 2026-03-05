using DragonEnvelopes.Application.Interfaces;

namespace DragonEnvelopes.Application.Services;

public sealed class HealthPingService(IClock clock) : IHealthPingService
{
    public DateTimeOffset GetUtcNow() => clock.UtcNow;
}

