using Moq;
using DragonEnvelopes.Application.Interfaces;

namespace DragonEnvelopes.Application.Tests.Fixtures;

public sealed class ApplicationTestFixture
{
    public DateTimeOffset FrozenUtcNow { get; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public Mock<IClock> CreateClockMock()
    {
        var mock = new Mock<IClock>();
        mock.SetupGet(x => x.UtcNow).Returns(FrozenUtcNow);
        return mock;
    }
}


