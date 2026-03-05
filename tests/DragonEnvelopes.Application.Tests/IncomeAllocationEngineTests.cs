using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public class IncomeAllocationEngineTests
{
    [Fact]
    public async Task AllocateAsync_MixedFixedAndPercent_AllocatesInPriorityOrder()
    {
        var repository = new Mock<IAutomationRuleRepository>();
        var familyId = Guid.NewGuid();
        var envelopeA = Guid.NewGuid();
        var envelopeB = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        repository.Setup(x => x.ListAsync(familyId, AutomationRuleType.Allocation, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                BuildRule(familyId, 1, now, "{}", $$"""{"targetEnvelopeId":"{{envelopeA}}","allocationType":"FixedAmount","value":200}"""),
                BuildRule(familyId, 2, now.AddMinutes(1), "{}", $$"""{"targetEnvelopeId":"{{envelopeB}}","allocationType":"Percent","value":25}""")
            ]);

        var engine = new IncomeAllocationEngine(repository.Object);
        var splits = await engine.AllocateAsync(familyId, "Paycheck", "Employer", 1000m, "Income");

        Assert.Equal(2, splits.Count);
        Assert.Equal(200m, splits[0].Amount);
        Assert.Equal(250m, splits[1].Amount);
    }

    [Fact]
    public async Task AllocateAsync_PercentUsesCurrencyRounding()
    {
        var repository = new Mock<IAutomationRuleRepository>();
        var familyId = Guid.NewGuid();
        var envelope = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        repository.Setup(x => x.ListAsync(familyId, AutomationRuleType.Allocation, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                BuildRule(familyId, 1, now, "{}", $$"""{"targetEnvelopeId":"{{envelope}}","allocationType":"Percent","value":33.333}""")
            ]);

        var engine = new IncomeAllocationEngine(repository.Object);
        var splits = await engine.AllocateAsync(familyId, "Paycheck", "Employer", 100m, "Income");

        Assert.Single(splits);
        Assert.Equal(33.33m, splits[0].Amount);
    }

    [Fact]
    public async Task AllocateAsync_WhenRulesExceedAmount_CapsAtRemaining()
    {
        var repository = new Mock<IAutomationRuleRepository>();
        var familyId = Guid.NewGuid();
        var envelopeA = Guid.NewGuid();
        var envelopeB = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        repository.Setup(x => x.ListAsync(familyId, AutomationRuleType.Allocation, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                BuildRule(familyId, 1, now, "{}", $$"""{"targetEnvelopeId":"{{envelopeA}}","allocationType":"FixedAmount","value":80}"""),
                BuildRule(familyId, 2, now.AddMinutes(1), "{}", $$"""{"targetEnvelopeId":"{{envelopeB}}","allocationType":"FixedAmount","value":50}""")
            ]);

        var engine = new IncomeAllocationEngine(repository.Object);
        var splits = await engine.AllocateAsync(familyId, "Paycheck", "Employer", 100m, "Income");

        Assert.Equal(2, splits.Count);
        Assert.Equal(80m, splits[0].Amount);
        Assert.Equal(20m, splits[1].Amount);
        Assert.Equal(100m, splits.Sum(static x => x.Amount));
    }

    private static AutomationRule BuildRule(
        Guid familyId,
        int priority,
        DateTimeOffset createdAt,
        string conditions,
        string action)
    {
        return new AutomationRule(
            Guid.NewGuid(),
            familyId,
            $"Rule {priority}",
            AutomationRuleType.Allocation,
            priority,
            true,
            conditions,
            action,
            createdAt,
            createdAt);
    }
}
