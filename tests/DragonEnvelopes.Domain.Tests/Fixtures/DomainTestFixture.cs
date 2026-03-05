namespace DragonEnvelopes.Domain.Tests.Fixtures;

public sealed class DomainTestFixture
{
    public DateTimeOffset FrozenUtcNow { get; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
}


