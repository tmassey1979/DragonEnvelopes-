using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Application.Tests.Fixtures;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public class FamilyServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsFamilyAndUsesClock()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var repository = new Mock<IFamilyRepository>();

        repository.Setup(x => x.FamilyNameExistsAsync("Massey Household", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Family? persistedFamily = null;
        repository.Setup(x => x.AddFamilyAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()))
            .Callback<Family, CancellationToken>((family, _) => persistedFamily = family)
            .Returns(Task.CompletedTask);

        var service = new FamilyService(repository.Object, clock.Object);

        var result = await service.CreateAsync("Massey Household");

        Assert.Equal("Massey Household", result.Name);
        Assert.Equal(fixture.FrozenUtcNow, result.CreatedAt);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Empty(result.Members);
        Assert.NotNull(persistedFamily);
        Assert.Equal(fixture.FrozenUtcNow, persistedFamily!.CreatedAt);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenFamilyNameExists()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var repository = new Mock<IFamilyRepository>();
        repository.Setup(x => x.FamilyNameExistsAsync("Existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new FamilyService(repository.Object, clock.Object);

        await Assert.ThrowsAsync<DomainValidationException>(() => service.CreateAsync("Existing"));
    }
}
