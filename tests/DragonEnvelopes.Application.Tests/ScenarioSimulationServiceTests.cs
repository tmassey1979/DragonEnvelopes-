using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Application.Tests.Fixtures;
using DragonEnvelopes.Domain;

namespace DragonEnvelopes.Application.Tests;

public class ScenarioSimulationServiceTests
{
    [Fact]
    public async Task SimulateAsync_ProjectsBalanceAndSetsDepletionMonth()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var service = new ScenarioSimulationService(clock.Object);
        var familyId = Guid.NewGuid();

        var result = await service.SimulateAsync(
            familyId,
            monthlyIncome: 1000m,
            fixedExpenses: 1500m,
            discretionaryCutPercent: null,
            monthHorizon: 3,
            startingBalance: 500m);

        Assert.Equal(familyId, result.FamilyId);
        Assert.Equal(3, result.Months.Count);
        Assert.Equal(2, result.DepletionMonth);
        Assert.Equal("2026-01", result.Months[0].Month);
        Assert.Equal(0m, result.Months[0].ProjectedBalance);
        Assert.Equal(-500m, result.Months[1].ProjectedBalance);
        Assert.Equal(-1000m, result.EndingBalance);
    }

    [Fact]
    public async Task SimulateAsync_AppliesDiscretionaryCutAndRoundsToCurrency()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var service = new ScenarioSimulationService(clock.Object);

        var result = await service.SimulateAsync(
            Guid.NewGuid(),
            monthlyIncome: 1000m,
            fixedExpenses: 1234.56m,
            discretionaryCutPercent: 10m,
            monthHorizon: 2,
            startingBalance: 1000m);

        Assert.Equal(1111.10m, result.EffectiveExpenses);
        Assert.Equal(-111.10m, result.NetMonthlyChange);
        Assert.Equal(777.80m, result.EndingBalance);
    }

    [Fact]
    public async Task SimulateAsync_ThrowsForInvalidMonthHorizon()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var service = new ScenarioSimulationService(clock.Object);

        await Assert.ThrowsAsync<DomainValidationException>(() => service.SimulateAsync(
            Guid.NewGuid(),
            monthlyIncome: 1000m,
            fixedExpenses: 900m,
            discretionaryCutPercent: null,
            monthHorizon: 0,
            startingBalance: 100m));
    }
}
