using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Application.Tests.Fixtures;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public class AutomationRuleServiceTests
{
    [Fact]
    public async Task CreateAsync_ReturnsCreatedRule()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var repository = new Mock<IAutomationRuleRepository>();
        var familyId = Guid.NewGuid();

        repository.Setup(x => x.FamilyExistsAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(x => x.AddAsync(It.IsAny<AutomationRule>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new AutomationRuleService(repository.Object, clock.Object);
        var result = await service.CreateAsync(
            familyId,
            "Food Rule",
            "Categorization",
            1,
            true,
            """{"merchantContains":"market"}""",
            """{"setCategory":"Food"}""");

        Assert.Equal(familyId, result.FamilyId);
        Assert.Equal("Categorization", result.RuleType);
        Assert.Equal(1, result.Priority);
    }

    [Fact]
    public async Task CreateAsync_ThrowsForInvalidJson()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var repository = new Mock<IAutomationRuleRepository>();
        var familyId = Guid.NewGuid();

        repository.Setup(x => x.FamilyExistsAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new AutomationRuleService(repository.Object, clock.Object);

        await Assert.ThrowsAsync<DomainValidationException>(() => service.CreateAsync(
            familyId,
            "Bad Rule",
            "Categorization",
            1,
            true,
            "not-json",
            """{"setCategory":"Food"}"""));
    }

    [Fact]
    public async Task ListAsync_ReturnsRulesInPriorityThenCreatedOrder()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var repository = new Mock<IAutomationRuleRepository>();
        var familyId = Guid.NewGuid();
        var first = new AutomationRule(
            Guid.NewGuid(),
            familyId,
            "First",
            AutomationRuleType.Categorization,
            1,
            true,
            "{}",
            "{}",
            fixture.FrozenUtcNow,
            fixture.FrozenUtcNow);
        var second = new AutomationRule(
            Guid.NewGuid(),
            familyId,
            "Second",
            AutomationRuleType.Categorization,
            2,
            true,
            "{}",
            "{}",
            fixture.FrozenUtcNow.AddMinutes(1),
            fixture.FrozenUtcNow.AddMinutes(1));

        repository.Setup(x => x.ListAsync(familyId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([first, second]);

        var service = new AutomationRuleService(repository.Object, clock.Object);
        var results = await service.ListAsync(familyId, null, null);

        Assert.Equal(2, results.Count);
        Assert.Equal("First", results[0].Name);
        Assert.Equal("Second", results[1].Name);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsWhenMissing()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var repository = new Mock<IAutomationRuleRepository>();
        var ruleId = Guid.NewGuid();

        repository.Setup(x => x.GetByIdForUpdateAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AutomationRule?)null);

        var service = new AutomationRuleService(repository.Object, clock.Object);

        await Assert.ThrowsAsync<DomainValidationException>(() => service.DeleteAsync(ruleId));
    }

    [Fact]
    public async Task DisableAsync_EnqueuesAutomationOutboxMessage()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var repository = new Mock<IAutomationRuleRepository>();
        var familyId = Guid.NewGuid();
        var rule = new AutomationRule(
            Guid.NewGuid(),
            familyId,
            "Disable Rule",
            AutomationRuleType.Categorization,
            1,
            true,
            "{}",
            "{}",
            fixture.FrozenUtcNow,
            fixture.FrozenUtcNow);
        var outboxRepository = new Mock<IIntegrationOutboxRepository>();

        repository.Setup(x => x.GetByIdForUpdateAsync(rule.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new AutomationRuleService(
            repository.Object,
            clock.Object,
            outboxRepository.Object);

        await service.DisableAsync(rule.Id);

        outboxRepository.Verify(
            x => x.AddAsync(
                It.Is<IntegrationOutboxMessage>(message =>
                    message.SourceService == "automation-api"
                    && message.RoutingKey == "automation.rule.disabled.v1"
                    && message.EventName == "AutomationRuleDisabled"
                    && message.FamilyId == familyId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
