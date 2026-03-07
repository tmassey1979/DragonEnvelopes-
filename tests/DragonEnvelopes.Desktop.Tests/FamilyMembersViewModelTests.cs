using System.Diagnostics;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Tests;

public sealed class FamilyMembersViewModelTests
{
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
