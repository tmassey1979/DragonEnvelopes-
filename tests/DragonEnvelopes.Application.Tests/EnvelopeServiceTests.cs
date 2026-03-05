using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public class EnvelopeServiceTests
{
    [Fact]
    public async Task CreateAsync_ReturnsCreatedEnvelope()
    {
        var repository = new Mock<IEnvelopeRepository>();
        var familyId = Guid.NewGuid();
        repository.Setup(x => x.FamilyExistsAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(x => x.EnvelopeNameExistsAsync(
                familyId,
                "Groceries",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(x => x.AddEnvelopeAsync(It.IsAny<Envelope>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new EnvelopeService(repository.Object);
        var envelope = await service.CreateAsync(familyId, "Groceries", 500m);

        Assert.Equal(familyId, envelope.FamilyId);
        Assert.Equal("Groceries", envelope.Name);
        Assert.Equal(500m, envelope.MonthlyBudget);
        Assert.False(envelope.IsArchived);
    }

    [Fact]
    public async Task UpdateAsync_ArchivesEnvelope()
    {
        var familyId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var envelope = new Envelope(
            envelopeId,
            familyId,
            "Utilities",
            Money.FromDecimal(200m),
            Money.FromDecimal(50m));

        var repository = new Mock<IEnvelopeRepository>();
        repository.Setup(x => x.GetByIdForUpdateAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(envelope);
        repository.Setup(x => x.EnvelopeNameExistsAsync(
                familyId,
                "Utilities",
                envelopeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new EnvelopeService(repository.Object);
        var updated = await service.UpdateAsync(envelopeId, "Utilities", 250m, true);

        Assert.Equal(250m, updated.MonthlyBudget);
        Assert.True(updated.IsArchived);
    }

    [Fact]
    public async Task ArchiveAsync_ThrowsWhenMissing()
    {
        var envelopeId = Guid.NewGuid();
        var repository = new Mock<IEnvelopeRepository>();
        repository.Setup(x => x.GetByIdForUpdateAsync(envelopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Envelope?)null);

        var service = new EnvelopeService(repository.Object);

        await Assert.ThrowsAsync<DomainValidationException>(() => service.ArchiveAsync(envelopeId));
    }
}
