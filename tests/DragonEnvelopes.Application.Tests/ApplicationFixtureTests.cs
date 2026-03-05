using DragonEnvelopes.Application.Tests.Fixtures;

namespace DragonEnvelopes.Application.Tests;

public class ApplicationFixtureTests
{
    [Fact]
    public void ClockMock_ReturnsFrozenTime()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();

        Assert.Equal(fixture.FrozenUtcNow, clock.Object.UtcNow);
    }
}


