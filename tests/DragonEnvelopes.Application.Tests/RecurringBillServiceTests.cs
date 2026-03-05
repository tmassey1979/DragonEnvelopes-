using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public class RecurringBillServiceTests
{
    [Fact]
    public async Task CreateUpdateDelete_Lifecycle_Works()
    {
        var repository = new Mock<IRecurringBillRepository>();
        var familyId = Guid.NewGuid();
        RecurringBill? stored = null;

        repository.Setup(x => x.FamilyExistsAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(x => x.AddAsync(It.IsAny<RecurringBill>(), It.IsAny<CancellationToken>()))
            .Callback<RecurringBill, CancellationToken>((bill, _) => stored = bill)
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => stored);
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.DeleteAsync(It.IsAny<RecurringBill>(), It.IsAny<CancellationToken>()))
            .Callback<RecurringBill, CancellationToken>((_, _) => stored = null)
            .Returns(Task.CompletedTask);

        var service = new RecurringBillService(repository.Object);
        var created = await service.CreateAsync(
            familyId,
            "Rent",
            "Landlord",
            1200m,
            "Monthly",
            1,
            new DateOnly(2026, 1, 1),
            null,
            true);

        var updated = await service.UpdateAsync(
            created.Id,
            "Rent Updated",
            "Landlord",
            1300m,
            "Monthly",
            2,
            new DateOnly(2026, 1, 1),
            null,
            true);

        await service.DeleteAsync(created.Id);

        Assert.Equal("Rent Updated", updated.Name);
        Assert.Equal(1300m, updated.Amount);
        Assert.Equal(2, updated.DayOfMonth);
        Assert.Null(stored);
    }

    [Fact]
    public async Task ProjectAsync_MonthlyDay31_UsesLastDayForShortMonths()
    {
        var repository = new Mock<IRecurringBillRepository>();
        var familyId = Guid.NewGuid();
        var bill = new RecurringBill(
            Guid.NewGuid(),
            familyId,
            "Card",
            "Card Co",
            Money.FromDecimal(55m),
            RecurringBillFrequency.Monthly,
            31,
            new DateOnly(2026, 1, 1),
            null,
            true);

        repository.Setup(x => x.ListByFamilyAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([bill]);

        var service = new RecurringBillService(repository.Object);
        var projection = await service.ProjectAsync(
            familyId,
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 3, 31));

        Assert.Equal(2, projection.Count);
        Assert.Equal(new DateOnly(2026, 2, 28), projection[0].DueDate);
        Assert.Equal(new DateOnly(2026, 3, 31), projection[1].DueDate);
    }

    [Fact]
    public async Task ProjectAsync_RespectsDateBoundsAndInactiveBills()
    {
        var repository = new Mock<IRecurringBillRepository>();
        var familyId = Guid.NewGuid();
        var active = new RecurringBill(
            Guid.NewGuid(),
            familyId,
            "Water",
            "Water Co",
            Money.FromDecimal(40m),
            RecurringBillFrequency.Monthly,
            10,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 2, 15),
            true);
        var inactive = new RecurringBill(
            Guid.NewGuid(),
            familyId,
            "Internet",
            "ISP",
            Money.FromDecimal(90m),
            RecurringBillFrequency.Monthly,
            5,
            new DateOnly(2026, 1, 1),
            null,
            false);

        repository.Setup(x => x.ListByFamilyAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([active, inactive]);

        var service = new RecurringBillService(repository.Object);
        var projection = await service.ProjectAsync(
            familyId,
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 3, 31));

        Assert.Single(projection);
        Assert.Equal("Water", projection[0].Name);
        Assert.Equal(new DateOnly(2026, 2, 10), projection[0].DueDate);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsWhenMissing()
    {
        var repository = new Mock<IRecurringBillRepository>();
        repository.Setup(x => x.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringBill?)null);

        var service = new RecurringBillService(repository.Object);
        await Assert.ThrowsAsync<DomainValidationException>(() => service.UpdateAsync(
            Guid.NewGuid(),
            "Name",
            "Merchant",
            10m,
            "Monthly",
            1,
            new DateOnly(2026, 1, 1),
            null,
            true));
    }
}
