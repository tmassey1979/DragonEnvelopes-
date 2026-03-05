using DragonEnvelopes.Domain.Tests.Fixtures;

namespace DragonEnvelopes.Domain.Tests;

public class DomainFixtureTests
{
    [Fact]
    public void FrozenUtcNow_IsStable()
    {
        var fixture = new DomainTestFixture();

        Assert.Equal(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), fixture.FrozenUtcNow);
    }
}


