using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public class BudgetServiceTests
{
    [Fact]
    public async Task CreateAsync_ReturnsCreatedBudget()
    {
        var repository = new Mock<IBudgetRepository>();
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var calculator = new RemainingBudgetCalculator();
        var familyId = Guid.NewGuid();
        repository.Setup(x => x.FamilyExistsAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(x => x.ExistsForMonthAsync(
                familyId,
                BudgetMonth.Parse("2026-03"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(x => x.AddAsync(It.IsAny<Budget>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        envelopeRepository.Setup(x => x.ListByFamilyAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new BudgetService(repository.Object, envelopeRepository.Object, calculator);
        var budget = await service.CreateAsync(familyId, "2026-03", 8000m);

        Assert.Equal(familyId, budget.FamilyId);
        Assert.Equal("2026-03", budget.Month);
        Assert.Equal(8000m, budget.TotalIncome);
        Assert.Equal(0m, budget.AllocatedAmount);
        Assert.Equal(8000m, budget.RemainingAmount);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsWhenNotFound()
    {
        var repository = new Mock<IBudgetRepository>();
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var calculator = new RemainingBudgetCalculator();
        var budgetId = Guid.NewGuid();
        repository.Setup(x => x.GetByIdForUpdateAsync(budgetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Budget?)null);

        var service = new BudgetService(repository.Object, envelopeRepository.Object, calculator);

        await Assert.ThrowsAsync<DomainValidationException>(() => service.UpdateAsync(budgetId, 9000m));
    }

    [Fact]
    public async Task GetByMonthAsync_ComputesRemainingFromActiveEnvelopeBudgets()
    {
        var repository = new Mock<IBudgetRepository>();
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var calculator = new RemainingBudgetCalculator();
        var familyId = Guid.NewGuid();
        var budget = new Budget(Guid.NewGuid(), familyId, BudgetMonth.Parse("2026-03"), Money.FromDecimal(1000m));

        repository.Setup(x => x.GetByFamilyAndMonthAsync(familyId, BudgetMonth.Parse("2026-03"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);
        envelopeRepository.Setup(x => x.ListByFamilyAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Envelope(Guid.NewGuid(), familyId, "Groceries", Money.FromDecimal(300m), Money.Zero),
                new Envelope(Guid.NewGuid(), familyId, "Dining", Money.FromDecimal(200m), Money.Zero),
                CreateArchivedEnvelope(familyId, "Archived", 400m)
            ]);

        var service = new BudgetService(repository.Object, envelopeRepository.Object, calculator);
        var result = await service.GetByMonthAsync(familyId, "2026-03");

        Assert.NotNull(result);
        Assert.Equal(500m, result!.AllocatedAmount);
        Assert.Equal(500m, result.RemainingAmount);
    }

    [Fact]
    public async Task CreateAsync_EnqueuesPlanningOutboxMessage()
    {
        var repository = new Mock<IBudgetRepository>();
        var envelopeRepository = new Mock<IEnvelopeRepository>();
        var outboxRepository = new Mock<IIntegrationOutboxRepository>();
        var calculator = new RemainingBudgetCalculator();
        var familyId = Guid.NewGuid();
        repository.Setup(x => x.FamilyExistsAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(x => x.ExistsForMonthAsync(
                familyId,
                BudgetMonth.Parse("2026-03"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(x => x.AddAsync(It.IsAny<Budget>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        envelopeRepository.Setup(x => x.ListByFamilyAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new BudgetService(
            repository.Object,
            envelopeRepository.Object,
            calculator,
            outboxRepository.Object);

        await service.CreateAsync(familyId, "2026-03", 1234m);

        outboxRepository.Verify(
            x => x.AddAsync(
                It.Is<DragonEnvelopes.Domain.Entities.IntegrationOutboxMessage>(message =>
                    message.SourceService == "planning-api"
                    && message.RoutingKey == "planning.budget.created.v1"
                    && message.EventName == "BudgetCreated"
                    && message.FamilyId == familyId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Envelope CreateArchivedEnvelope(Guid familyId, string name, decimal monthlyBudget)
    {
        var envelope = new Envelope(Guid.NewGuid(), familyId, name, Money.FromDecimal(monthlyBudget), Money.Zero);
        envelope.Archive();
        return envelope;
    }
}
