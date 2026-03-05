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

        var service = new BudgetService(repository.Object);
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
        var budgetId = Guid.NewGuid();
        repository.Setup(x => x.GetByIdForUpdateAsync(budgetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Budget?)null);

        var service = new BudgetService(repository.Object);

        await Assert.ThrowsAsync<DomainValidationException>(() => service.UpdateAsync(budgetId, 9000m));
    }
}
