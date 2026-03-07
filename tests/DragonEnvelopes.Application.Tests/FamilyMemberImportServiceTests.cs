using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public sealed class FamilyMemberImportServiceTests
{
    [Fact]
    public async Task PreviewAsync_Flags_Duplicates_And_RowValidationErrors()
    {
        var familyId = Guid.NewGuid();
        var family = new Family(familyId, "Import Family", DateTimeOffset.UtcNow);
        var repository = new Mock<IFamilyRepository>();
        repository.Setup(x => x.GetFamilyByIdAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(family);
        repository.Setup(x => x.ListMembersAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new FamilyMember(
                    Guid.NewGuid(),
                    familyId,
                    "existing-kc",
                    "Existing",
                    EmailAddress.Parse("existing@test.dev"),
                    MemberRole.Parent)
            ]);

        var service = new FamilyMemberImportService(repository.Object);

        var csv = string.Join('\n',
        [
            "keycloakUserId,name,email,role",
            "existing-kc,Alpha,alpha@test.dev,Adult",
            "kc-2,Beta,existing@test.dev,Teen",
            "kc-3,Gamma,gamma@test.dev,Unknown",
            "kc-4,Delta,delta@test.dev,Child"
        ]);

        var preview = await service.PreviewAsync(familyId, csv, null, null);

        Assert.Equal(4, preview.Parsed);
        Assert.Equal(1, preview.Valid);
        Assert.Equal(2, preview.Deduped);
        Assert.Contains(preview.Rows, row => row.RowNumber == 2 && row.Errors.Any(static error => error.Contains("Duplicate keycloak user id", StringComparison.OrdinalIgnoreCase)));
        Assert.Contains(preview.Rows, row => row.RowNumber == 3 && row.Errors.Any(static error => error.Contains("Duplicate email", StringComparison.OrdinalIgnoreCase)));
        Assert.Contains(preview.Rows, row => row.RowNumber == 4 && row.Errors.Any(static error => error.Contains("Role is invalid", StringComparison.OrdinalIgnoreCase)));
        Assert.Contains(preview.Rows, row => row.RowNumber == 5 && row.Errors.Count == 0 && !row.IsDuplicate);
    }

    [Fact]
    public async Task CommitAsync_Inserts_Only_Valid_NonDuplicate_Rows()
    {
        var familyId = Guid.NewGuid();
        var family = new Family(familyId, "Import Family", DateTimeOffset.UtcNow);
        var repository = new Mock<IFamilyRepository>();
        repository.Setup(x => x.GetFamilyByIdAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(family);
        repository.Setup(x => x.ListMembersAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<FamilyMember>());

        var addedMembers = new List<FamilyMember>();
        repository.Setup(x => x.AddMemberAsync(It.IsAny<FamilyMember>(), It.IsAny<CancellationToken>()))
            .Callback<FamilyMember, CancellationToken>((member, _) => addedMembers.Add(member))
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new FamilyMemberImportService(repository.Object);

        var csv = string.Join('\n',
        [
            "keycloakUserId,name,email,role",
            "kc-1,Alpha,alpha@test.dev,Adult",
            "kc-2,Beta,beta@test.dev,Teen",
            "kc-3,Gamma,beta@test.dev,Child"
        ]);

        var commit = await service.CommitAsync(familyId, csv, null, null, acceptedRowNumbers: null);

        Assert.Equal(3, commit.Parsed);
        Assert.Equal(2, commit.Valid);
        Assert.Equal(1, commit.Deduped);
        Assert.Equal(2, commit.Inserted);
        Assert.Equal(1, commit.Failed);
        Assert.Equal(2, addedMembers.Count);
        Assert.Contains(addedMembers, member => member.KeycloakUserId == "kc-1" && member.Role == MemberRole.Adult);
        Assert.Contains(addedMembers, member => member.KeycloakUserId == "kc-2" && member.Role == MemberRole.Teen);
    }

    [Fact]
    public async Task PreviewAsync_Throws_When_Family_Not_Found()
    {
        var repository = new Mock<IFamilyRepository>();
        repository.Setup(x => x.GetFamilyByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Family?)null);

        var service = new FamilyMemberImportService(repository.Object);

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            service.PreviewAsync(Guid.NewGuid(), "keycloakUserId,name,email,role\nkc-1,A,a@test.dev,Adult", null, null));
    }
}
