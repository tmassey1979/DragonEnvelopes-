using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Application.Tests.Fixtures;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
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
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(repository.Object, clock);

        var result = await service.CreateAsync("Massey Household");

        Assert.Equal("Massey Household", result.Name);
        Assert.Equal(fixture.FrozenUtcNow, result.CreatedAt);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Empty(result.Members);
        Assert.NotNull(persistedFamily);
        Assert.Equal(fixture.FrozenUtcNow, persistedFamily!.CreatedAt);
        Assert.Equal("USD", persistedFamily.CurrencyCode);
        Assert.Equal("America/Chicago", persistedFamily.TimeZoneId);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenFamilyNameExists()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var repository = new Mock<IFamilyRepository>();
        repository.Setup(x => x.FamilyNameExistsAsync("Existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService(repository.Object, clock);

        await Assert.ThrowsAsync<DomainValidationException>(() => service.CreateAsync("Existing"));
    }

    [Fact]
    public async Task AddMemberAsync_ReturnsCreatedMember()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var repository = new Mock<IFamilyRepository>();
        var familyId = Guid.NewGuid();
        var family = new Family(familyId, "Household", fixture.FrozenUtcNow);

        repository.Setup(x => x.GetFamilyByIdAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(family);
        repository.Setup(x => x.MemberKeycloakUserIdExistsAsync(familyId, "kc-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(x => x.AddMemberAsync(It.IsAny<FamilyMember>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(repository.Object, clock);

        var member = await service.AddMemberAsync(
            familyId,
            "kc-123",
            "Terry",
            "terry@example.com",
            "Parent");

        Assert.Equal(familyId, member.FamilyId);
        Assert.Equal("kc-123", member.KeycloakUserId);
        Assert.Equal("Parent", member.Role);
    }

    [Fact]
    public async Task AddMemberAsync_ThrowsWhenRoleInvalid()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var repository = new Mock<IFamilyRepository>();
        var familyId = Guid.NewGuid();
        var family = new Family(familyId, "Household", fixture.FrozenUtcNow);

        repository.Setup(x => x.GetFamilyByIdAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(family);
        repository.Setup(x => x.MemberKeycloakUserIdExistsAsync(familyId, "kc-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(repository.Object, clock);

        await Assert.ThrowsAsync<DomainValidationException>(() => service.AddMemberAsync(
            familyId,
            "kc-123",
            "Terry",
            "terry@example.com",
            "UnknownRole"));
    }

    [Fact]
    public async Task UpdateProfileAsync_Updates_Profile_Fields_And_Saves()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var repository = new Mock<IFamilyRepository>();
        var familyId = Guid.NewGuid();
        var family = new Family(familyId, "Household", fixture.FrozenUtcNow);

        repository.Setup(x => x.GetFamilyByIdForUpdateAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(family);
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(repository.Object, clock);

        var updated = await service.UpdateProfileAsync(
            familyId,
            "Household Prime",
            "EUR",
            "Europe/Berlin");

        Assert.Equal("Household Prime", updated.Name);
        Assert.Equal("EUR", updated.CurrencyCode);
        Assert.Equal("Europe/Berlin", updated.TimeZoneId);
        Assert.Equal(fixture.FrozenUtcNow, updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_UpdatesRoleAndSaves()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var repository = new Mock<IFamilyRepository>();
        var familyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var family = new Family(familyId, "Household", fixture.FrozenUtcNow);
        var member = new FamilyMember(
            memberId,
            familyId,
            "member-role-update",
            "Member Update",
            EmailAddress.Parse("member-update@test.dev"),
            MemberRole.Adult);

        repository.Setup(x => x.GetFamilyByIdAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(family);
        repository.Setup(x => x.GetMemberByIdForUpdateAsync(familyId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(repository.Object, clock);

        var updated = await service.UpdateMemberRoleAsync(familyId, memberId, "Teen");

        Assert.Equal("Teen", updated.Role);
        Assert.Equal(MemberRole.Teen, member.Role);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_Throws_WhenDemotingLastParent()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var repository = new Mock<IFamilyRepository>();
        var familyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var family = new Family(familyId, "Household", fixture.FrozenUtcNow);
        var member = new FamilyMember(
            memberId,
            familyId,
            "member-last-parent",
            "Last Parent",
            EmailAddress.Parse("last-parent@test.dev"),
            MemberRole.Parent);

        repository.Setup(x => x.GetFamilyByIdAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(family);
        repository.Setup(x => x.GetMemberByIdForUpdateAsync(familyId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        repository.Setup(x => x.CountMembersByRoleAsync(familyId, MemberRole.Parent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService(repository.Object, clock);

        await Assert.ThrowsAsync<DomainValidationException>(() => service.UpdateMemberRoleAsync(familyId, memberId, "Adult"));
    }

    [Fact]
    public async Task RemoveMemberAsync_RemovesMemberAndSaves()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var repository = new Mock<IFamilyRepository>();
        var familyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var family = new Family(familyId, "Household", fixture.FrozenUtcNow);
        var member = new FamilyMember(
            memberId,
            familyId,
            "member-remove",
            "Member Remove",
            EmailAddress.Parse("member-remove@test.dev"),
            MemberRole.Adult);

        repository.Setup(x => x.GetFamilyByIdAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(family);
        repository.Setup(x => x.GetMemberByIdForUpdateAsync(familyId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        repository.Setup(x => x.RemoveMemberAsync(member, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(repository.Object, clock);

        await service.RemoveMemberAsync(familyId, memberId);

        repository.Verify(x => x.RemoveMemberAsync(member, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveMemberAsync_Throws_WhenRemovingLastParent()
    {
        var fixture = new ApplicationTestFixture();
        var clock = fixture.CreateClockMock();
        var repository = new Mock<IFamilyRepository>();
        var familyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var family = new Family(familyId, "Household", fixture.FrozenUtcNow);
        var member = new FamilyMember(
            memberId,
            familyId,
            "member-last-parent-remove",
            "Last Parent Remove",
            EmailAddress.Parse("last-parent-remove@test.dev"),
            MemberRole.Parent);

        repository.Setup(x => x.GetFamilyByIdAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(family);
        repository.Setup(x => x.GetMemberByIdForUpdateAsync(familyId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        repository.Setup(x => x.CountMembersByRoleAsync(familyId, MemberRole.Parent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService(repository.Object, clock);

        await Assert.ThrowsAsync<DomainValidationException>(() => service.RemoveMemberAsync(familyId, memberId));
    }

    private static FamilyService CreateService(IFamilyRepository repository, Mock<IClock> clock)
    {
        var outboxRepository = new Mock<IIntegrationOutboxRepository>();
        outboxRepository.Setup(x => x.AddAsync(It.IsAny<IntegrationOutboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new FamilyService(repository, outboxRepository.Object, clock.Object);
    }
}
