using System.Diagnostics;
using DragonEnvelopes.Desktop.Services;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Tests;

public sealed class FamilyMembersViewModelTests
{
    [Fact]
    public async Task UpdateSelectedMemberRoleCommand_Updates_Selected_Member()
    {
        var familyMembersDataService = new FakeFamilyMembersDataService();
        var member = new FamilyMemberItemViewModel(
            Guid.NewGuid(),
            "member-role-update",
            "Role Update",
            "role-update@test.dev",
            "Adult");
        familyMembersDataService.Members.Add(member);

        var viewModel = new FamilyMembersViewModel(familyMembersDataService);
        await WaitForIdleAsync(viewModel);
        viewModel.SelectedMember = viewModel.Members.Single();
        viewModel.SelectedMemberRole = "Parent";

        await viewModel.UpdateSelectedMemberRoleCommand.ExecuteAsync(null);
        await WaitForIdleAsync(viewModel);

        Assert.False(viewModel.HasError);
        Assert.Equal(1, familyMembersDataService.UpdateMemberRoleCallCount);
        Assert.Equal("Parent", viewModel.SelectedMember?.Role);
        Assert.Contains("Updated role", viewModel.EditorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RemoveSelectedMemberCommand_RequiresConfirmationThenRemovesMember()
    {
        var familyMembersDataService = new FakeFamilyMembersDataService();
        familyMembersDataService.Members.Add(new FamilyMemberItemViewModel(
            Guid.NewGuid(),
            "member-remove-1",
            "Remove One",
            "remove-one@test.dev",
            "Adult"));
        familyMembersDataService.Members.Add(new FamilyMemberItemViewModel(
            Guid.NewGuid(),
            "member-remove-2",
            "Remove Two",
            "remove-two@test.dev",
            "Teen"));

        var viewModel = new FamilyMembersViewModel(familyMembersDataService);
        await WaitForIdleAsync(viewModel);
        viewModel.SelectedMember = viewModel.Members.First(member => member.Name == "Remove One");

        await viewModel.RemoveSelectedMemberCommand.ExecuteAsync(null);
        await WaitForIdleAsync(viewModel);
        Assert.Equal(0, familyMembersDataService.RemoveMemberCallCount);
        Assert.Contains("Click Remove Member again", viewModel.MemberRemovalStatus, StringComparison.OrdinalIgnoreCase);

        await viewModel.RemoveSelectedMemberCommand.ExecuteAsync(null);
        await WaitForIdleAsync(viewModel);

        Assert.False(viewModel.HasError);
        Assert.Equal(1, familyMembersDataService.RemoveMemberCallCount);
        Assert.DoesNotContain(viewModel.Members, member => member.Name == "Remove One");
        Assert.True(viewModel.CanUndoRemove);
    }

    [Fact]
    public async Task UndoRemoveMemberCommand_RestoresRecentlyRemovedMember()
    {
        var familyMembersDataService = new FakeFamilyMembersDataService();
        familyMembersDataService.Members.Add(new FamilyMemberItemViewModel(
            Guid.NewGuid(),
            "member-remove-undo",
            "Undo Member",
            "undo-member@test.dev",
            "Adult"));

        var viewModel = new FamilyMembersViewModel(familyMembersDataService);
        await WaitForIdleAsync(viewModel);
        viewModel.SelectedMember = viewModel.Members.Single();

        await viewModel.RemoveSelectedMemberCommand.ExecuteAsync(null);
        await WaitForIdleAsync(viewModel);
        await viewModel.RemoveSelectedMemberCommand.ExecuteAsync(null);
        await WaitForIdleAsync(viewModel);
        Assert.DoesNotContain(viewModel.Members, member => member.KeycloakUserId == "member-remove-undo");

        await viewModel.UndoRemoveMemberCommand.ExecuteAsync(null);
        await WaitForIdleAsync(viewModel);

        Assert.False(viewModel.HasError);
        Assert.Contains(viewModel.Members, member => member.KeycloakUserId == "member-remove-undo");
        Assert.False(viewModel.CanUndoRemove);
        Assert.Contains("Undo completed", viewModel.MemberRemovalStatus, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResendSelectedInviteCommand_Resends_Pending_Invite()
    {
        var familyMembersDataService = new FakeFamilyMembersDataService();
        familyMembersDataService.Invites.Add(new FamilyInviteItemViewModel(
            Guid.NewGuid(),
            "pending@test.dev",
            "Adult",
            "Pending",
            DateTimeOffset.UtcNow.AddDays(-1).ToString("yyyy-MM-dd HH:mm 'UTC'"),
            DateTimeOffset.UtcNow.AddDays(2).ToString("yyyy-MM-dd HH:mm 'UTC'")));

        var viewModel = new FamilyMembersViewModel(familyMembersDataService);
        await WaitForIdleAsync(viewModel);

        viewModel.DraftInviteExpiresInHours = "96";
        await viewModel.ResendInviteCommand.ExecuteAsync(null);
        await WaitForIdleAsync(viewModel);

        Assert.False(viewModel.HasError);
        Assert.Equal(1, familyMembersDataService.ResendInviteCallCount);
        Assert.Equal("resend-test-invite-token", viewModel.LastInviteToken);
        Assert.Equal("Invite resent for 'pending@test.dev'.", viewModel.InviteMessage);
    }

    [Fact]
    public async Task ResendSelectedInviteCommand_Rejects_NonPending_Invite()
    {
        var familyMembersDataService = new FakeFamilyMembersDataService();
        familyMembersDataService.Invites.Add(new FamilyInviteItemViewModel(
            Guid.NewGuid(),
            "cancelled@test.dev",
            "Adult",
            "Cancelled",
            DateTimeOffset.UtcNow.AddDays(-3).ToString("yyyy-MM-dd HH:mm 'UTC'"),
            DateTimeOffset.UtcNow.AddDays(-1).ToString("yyyy-MM-dd HH:mm 'UTC'")));

        var viewModel = new FamilyMembersViewModel(familyMembersDataService);
        await WaitForIdleAsync(viewModel);

        await viewModel.ResendInviteCommand.ExecuteAsync(null);
        await WaitForIdleAsync(viewModel);

        Assert.True(viewModel.HasError);
        Assert.Equal("Only pending invites can be resent.", viewModel.ErrorMessage);
        Assert.Equal(0, familyMembersDataService.ResendInviteCallCount);
    }

    [Fact]
    public async Task InviteTimeline_Filters_By_Email_And_EventType()
    {
        var familyMembersDataService = new FakeFamilyMembersDataService();
        var inviteId = Guid.NewGuid();
        familyMembersDataService.InviteTimeline.AddRange(
        [
            new FamilyInviteTimelineItemViewModel(
                Guid.NewGuid(),
                inviteId,
                "alpha@test.dev",
                "Created",
                "parent-a",
                DateTimeOffset.UtcNow.AddMinutes(-15)),
            new FamilyInviteTimelineItemViewModel(
                Guid.NewGuid(),
                inviteId,
                "alpha@test.dev",
                "Resent",
                "parent-a",
                DateTimeOffset.UtcNow.AddMinutes(-10)),
            new FamilyInviteTimelineItemViewModel(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "bravo@test.dev",
                "Redeemed",
                "redeemer-a",
                DateTimeOffset.UtcNow.AddMinutes(-5))
        ]);

        var viewModel = new FamilyMembersViewModel(familyMembersDataService);
        await WaitForIdleAsync(viewModel);

        Assert.Equal(1, familyMembersDataService.GetInviteTimelineCallCount);
        Assert.Equal(3, viewModel.InviteTimeline.Count);

        viewModel.InviteTimelineEmailFilter = "alpha@test.dev";
        Assert.Equal(2, viewModel.InviteTimeline.Count);

        viewModel.InviteTimelineEventTypeFilter = "Resent";
        Assert.Single(viewModel.InviteTimeline);
        Assert.Equal("Resent", viewModel.InviteTimeline.Single().EventType);
        Assert.Contains("1 timeline event", viewModel.InviteTimelineSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MemberImportPreview_And_Commit_Uses_ValidRows()
    {
        var familyMembersDataService = new FakeFamilyMembersDataService();
        familyMembersDataService.MemberImportPreviewRows.AddRange(
        [
            new FamilyMemberImportPreviewRowData(
                2,
                "import-kc-1",
                "Import One",
                "import-one@test.dev",
                "Adult",
                IsDuplicate: false,
                Errors: string.Empty),
            new FamilyMemberImportPreviewRowData(
                3,
                "import-kc-2",
                "Import Two",
                "import-two@test.dev",
                "Teen",
                IsDuplicate: true,
                Errors: "Duplicate email.")
        ]);

        var viewModel = new FamilyMembersViewModel(familyMembersDataService);
        await WaitForIdleAsync(viewModel);
        viewModel.MemberImportCsvContent = "keycloakUserId,name,email,role\nimport-kc-1,Import One,import-one@test.dev,Adult";

        await viewModel.PreviewMemberImportCommand.ExecuteAsync(null);
        await WaitForIdleAsync(viewModel);

        Assert.Equal(1, familyMembersDataService.PreviewMemberImportCallCount);
        Assert.Equal(2, viewModel.MemberImportPreviewRows.Count);
        Assert.Contains("parsed 2 row", viewModel.MemberImportPreviewSummary, StringComparison.OrdinalIgnoreCase);

        await viewModel.CommitMemberImportCommand.ExecuteAsync(null);
        await WaitForIdleAsync(viewModel);

        Assert.Equal(1, familyMembersDataService.CommitMemberImportCallCount);
        Assert.Contains("inserted 1", viewModel.MemberImportCommitSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(viewModel.Members, member => member.KeycloakUserId == "import-kc-1");
        Assert.DoesNotContain(viewModel.Members, member => member.KeycloakUserId == "import-kc-2");
    }

    private static async Task WaitForIdleAsync(FamilyMembersViewModel viewModel, int timeoutMilliseconds = 6000)
    {
        var stopwatch = Stopwatch.StartNew();
        while (viewModel.IsLoading)
        {
            if (stopwatch.ElapsedMilliseconds >= timeoutMilliseconds)
            {
                throw new TimeoutException("Timed out waiting for family members view model to become idle.");
            }

            await Task.Delay(20);
        }

        await Task.Delay(20);
    }
}
