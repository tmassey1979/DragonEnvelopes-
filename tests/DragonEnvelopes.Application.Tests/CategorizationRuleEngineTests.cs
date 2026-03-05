using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public class CategorizationRuleEngineTests
{
    [Fact]
    public async Task EvaluateAsync_FirstMatchWinsByPriority()
    {
        var repository = new Mock<IAutomationRuleRepository>();
        var familyId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        repository.Setup(x => x.ListAsync(familyId, AutomationRuleType.Categorization, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                BuildRule(familyId, "Second", 2, now.AddMinutes(1), """{"merchantContains":"market"}""", """{"setCategory":"Misc"}"""),
                BuildRule(familyId, "First", 1, now, """{"merchantContains":"market"}""", """{"setCategory":"Groceries"}""")
            ]);

        var engine = new CategorizationRuleEngine(repository.Object);
        var result = await engine.EvaluateAsync(familyId, "Store run", "Market Basket", -42m, null);

        Assert.Equal("Groceries", result);
    }

    [Fact]
    public async Task EvaluateAsync_TieBreaksByCreatedAtAscending()
    {
        var repository = new Mock<IAutomationRuleRepository>();
        var familyId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        repository.Setup(x => x.ListAsync(familyId, AutomationRuleType.Categorization, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                BuildRule(familyId, "Later", 1, now.AddMinutes(1), """{"descriptionContains":"paycheck"}""", """{"setCategory":"Bonus"}"""),
                BuildRule(familyId, "Earlier", 1, now, """{"descriptionContains":"paycheck"}""", """{"setCategory":"Income"}""")
            ]);

        var engine = new CategorizationRuleEngine(repository.Object);
        var result = await engine.EvaluateAsync(familyId, "Monthly paycheck", "Employer", 3000m, null);

        Assert.Equal("Income", result);
    }

    [Fact]
    public async Task EvaluateAsync_NoMatchReturnsNull()
    {
        var repository = new Mock<IAutomationRuleRepository>();
        var familyId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        repository.Setup(x => x.ListAsync(familyId, AutomationRuleType.Categorization, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                BuildRule(familyId, "Rule", 1, now, """{"merchantContains":"gas"}""", """{"setCategory":"Transport"}""")
            ]);

        var engine = new CategorizationRuleEngine(repository.Object);
        var result = await engine.EvaluateAsync(familyId, "Coffee run", "Cafe", -8m, null);

        Assert.Null(result);
    }

    private static AutomationRule BuildRule(
        Guid familyId,
        string name,
        int priority,
        DateTimeOffset createdAt,
        string conditions,
        string action)
    {
        return new AutomationRule(
            Guid.NewGuid(),
            familyId,
            name,
            AutomationRuleType.Categorization,
            priority,
            true,
            conditions,
            action,
            createdAt,
            createdAt);
    }
}
