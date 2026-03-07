using System.Diagnostics;
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
    public async Task RemoveSelectedMemberCommand_Removes_Selected_Member()
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

        Assert.False(viewModel.HasError);
        Assert.Equal(1, familyMembersDataService.RemoveMemberCallCount);
        Assert.DoesNotContain(viewModel.Members, member => member.Name == "Remove One");
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
