using DragonEnvelopes.Desktop.Services;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Tests;

internal sealed class FakeFamilyMembersDataService : IFamilyMembersDataService
{
    public List<FamilyMemberItemViewModel> Members { get; } = [];
    public List<FamilyInviteItemViewModel> Invites { get; } = [];
    public List<FamilyInviteTimelineItemViewModel> InviteTimeline { get; } = [];

    public int GetMembersCallCount { get; private set; }
    public int GetInvitesCallCount { get; private set; }
    public int GetInviteTimelineCallCount { get; private set; }
    public int CreateInviteCallCount { get; private set; }
    public int CancelInviteCallCount { get; private set; }
    public int ResendInviteCallCount { get; private set; }
    public int UpdateMemberRoleCallCount { get; private set; }
    public int RemoveMemberCallCount { get; private set; }

    public Task<IReadOnlyList<FamilyMemberItemViewModel>> GetMembersAsync(CancellationToken cancellationToken = default)
    {
        GetMembersCallCount += 1;
        return Task.FromResult<IReadOnlyList<FamilyMemberItemViewModel>>(Members.ToArray());
    }

    public Task<FamilyMemberItemViewModel> AddMemberAsync(
        string keycloakUserId,
        string name,
        string email,
        string role,
        CancellationToken cancellationToken = default)
    {
        var member = new FamilyMemberItemViewModel(Guid.NewGuid(), keycloakUserId, name, email, role);
        Members.Add(member);
        return Task.FromResult(member);
    }

    public Task<FamilyMemberItemViewModel> UpdateMemberRoleAsync(
        Guid memberId,
        string role,
        CancellationToken cancellationToken = default)
    {
        UpdateMemberRoleCallCount += 1;
        var existing = Members.FirstOrDefault(member => member.Id == memberId)
            ?? throw new InvalidOperationException("Member was not found.");
        var updated = existing with { Role = role };
        Members.Remove(existing);
        Members.Add(updated);
        return Task.FromResult(updated);
    }

    public Task RemoveMemberAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        RemoveMemberCallCount += 1;
        var existing = Members.FirstOrDefault(member => member.Id == memberId)
            ?? throw new InvalidOperationException("Member was not found.");
        Members.Remove(existing);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<FamilyInviteItemViewModel>> GetInvitesAsync(CancellationToken cancellationToken = default)
    {
        GetInvitesCallCount += 1;
        return Task.FromResult<IReadOnlyList<FamilyInviteItemViewModel>>(Invites.ToArray());
    }

    public Task<IReadOnlyList<FamilyInviteTimelineItemViewModel>> GetInviteTimelineAsync(
        string? emailFilter = null,
        string? eventTypeFilter = null,
        int take = 200,
        CancellationToken cancellationToken = default)
    {
        GetInviteTimelineCallCount += 1;
        var filtered = InviteTimeline.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(emailFilter))
        {
            var normalizedEmail = emailFilter.Trim();
            filtered = filtered.Where(item => item.Email.Contains(normalizedEmail, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(eventTypeFilter) && !eventTypeFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedEventType = eventTypeFilter.Trim();
            filtered = filtered.Where(item => item.EventType.Equals(normalizedEventType, StringComparison.OrdinalIgnoreCase));
        }

        var rows = filtered
            .OrderByDescending(static item => item.OccurredAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .ToArray();

        return Task.FromResult<IReadOnlyList<FamilyInviteTimelineItemViewModel>>(rows);
    }

    public Task<CreateFamilyInviteResultData> CreateInviteAsync(
        string email,
        string role,
        int expiresInHours,
        CancellationToken cancellationToken = default)
    {
        CreateInviteCallCount += 1;
        var invite = new FamilyInviteItemViewModel(
            Guid.NewGuid(),
            email,
            role,
            "Pending",
            DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'"),
            DateTimeOffset.UtcNow.AddHours(expiresInHours).ToString("yyyy-MM-dd HH:mm 'UTC'"));
        Invites.Add(invite);
        return Task.FromResult(new CreateFamilyInviteResultData(invite, "test-invite-token"));
    }

    public Task<FamilyInviteItemViewModel> CancelInviteAsync(Guid inviteId, CancellationToken cancellationToken = default)
    {
        CancelInviteCallCount += 1;
        var existing = Invites.FirstOrDefault(invite => invite.Id == inviteId)
            ?? throw new InvalidOperationException("Invite was not found.");
        Invites.Remove(existing);
        var cancelled = new FamilyInviteItemViewModel(
            existing.Id,
            existing.Email,
            existing.Role,
            "Cancelled",
            existing.CreatedAtUtc,
            existing.ExpiresAtUtc);
        Invites.Add(cancelled);
        return Task.FromResult(cancelled);
    }

    public Task<CreateFamilyInviteResultData> ResendInviteAsync(
        Guid inviteId,
        int expiresInHours,
        CancellationToken cancellationToken = default)
    {
        ResendInviteCallCount += 1;
        var existing = Invites.FirstOrDefault(invite => invite.Id == inviteId)
            ?? throw new InvalidOperationException("Invite was not found.");
        var resent = new FamilyInviteItemViewModel(
            existing.Id,
            existing.Email,
            existing.Role,
            "Pending",
            existing.CreatedAtUtc,
            DateTimeOffset.UtcNow.AddHours(expiresInHours).ToString("yyyy-MM-dd HH:mm 'UTC'"));
        Invites.Remove(existing);
        Invites.Add(resent);
        return Task.FromResult(new CreateFamilyInviteResultData(resent, "resend-test-invite-token"));
    }
}
