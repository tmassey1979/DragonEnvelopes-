using System.Security.Cryptography;
using System.Text;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class FamilyInviteService(
    IFamilyRepository familyRepository,
    IFamilyInviteRepository familyInviteRepository,
    IFamilyInviteSender familyInviteSender,
    IClock clock) : IFamilyInviteService
{
    private static readonly string[] AllowedRoles = ["Parent", "Adult", "Teen", "Child"];

    public async Task<CreateFamilyInviteResult> CreateAsync(
        Guid familyId,
        string email,
        string role,
        int expiresInHours,
        string? actorUserId = null,
        CancellationToken cancellationToken = default)
    {
        var family = await familyRepository.GetFamilyByIdAsync(familyId, cancellationToken);
        if (family is null)
        {
            throw new DomainValidationException("Family was not found.");
        }

        var normalizedRole = string.IsNullOrWhiteSpace(role) ? string.Empty : role.Trim();
        if (!AllowedRoles.Contains(normalizedRole, StringComparer.OrdinalIgnoreCase))
        {
            throw new DomainValidationException("Invite role is invalid.");
        }

        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();
        if (await familyInviteRepository.HasPendingInviteAsync(familyId, normalizedEmail, cancellationToken))
        {
            throw new DomainValidationException("A pending invite already exists for this email.");
        }

        var now = clock.UtcNow;
        var inviteToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var invite = new FamilyInvite(
            Guid.NewGuid(),
            familyId,
            normalizedEmail,
            normalizedRole,
            ComputeTokenHash(inviteToken),
            now,
            now.AddHours(Math.Clamp(expiresInHours, 1, 24 * 30)));

        await familyInviteRepository.AddAsync(invite, cancellationToken);
        await RecordTimelineEventAsync(
            invite,
            FamilyInviteTimelineEventType.Created,
            actorUserId,
            now,
            cancellationToken);

        try
        {
            await familyInviteSender.SendInviteAsync(
                familyId,
                invite.Email,
                invite.Role,
                inviteToken,
                invite.ExpiresAtUtc,
                cancellationToken);
        }
        catch
        {
            // Invite persistence should succeed even when outbound email fails.
        }

        return new CreateFamilyInviteResult(Map(invite), inviteToken);
    }

    public async Task<IReadOnlyList<FamilyInviteDetails>> ListByFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        var invites = await familyInviteRepository.ListByFamilyAsync(familyId, cancellationToken);
        var now = clock.UtcNow;
        var hasChanges = false;
        foreach (var invite in invites)
        {
            var priorStatus = invite.Status;
            invite.Expire(now);
            hasChanges |= priorStatus != invite.Status;
        }

        if (hasChanges)
        {
            await familyInviteRepository.SaveChangesAsync(cancellationToken);
        }

        return invites.Select(Map).ToArray();
    }

    public async Task<FamilyInviteDetails> CancelAsync(
        Guid inviteId,
        string? actorUserId = null,
        CancellationToken cancellationToken = default)
    {
        var invite = await familyInviteRepository.GetByIdForUpdateAsync(inviteId, cancellationToken)
            ?? throw new DomainValidationException("Invite was not found.");

        var now = clock.UtcNow;
        invite.Expire(now);
        invite.Cancel(now);
        await familyInviteRepository.SaveChangesAsync(cancellationToken);
        await RecordTimelineEventAsync(
            invite,
            FamilyInviteTimelineEventType.Cancelled,
            actorUserId,
            now,
            cancellationToken);
        return Map(invite);
    }

    public async Task<CreateFamilyInviteResult> ResendAsync(
        Guid inviteId,
        int expiresInHours,
        string? actorUserId = null,
        CancellationToken cancellationToken = default)
    {
        var invite = await familyInviteRepository.GetByIdForUpdateAsync(inviteId, cancellationToken)
            ?? throw new DomainValidationException("Invite was not found.");

        var now = clock.UtcNow;
        invite.Expire(now);
        if (invite.Status != FamilyInviteStatus.Pending)
        {
            throw new DomainValidationException("Only pending invites can be resent.");
        }

        var inviteToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        invite.Resend(
            ComputeTokenHash(inviteToken),
            now.AddHours(Math.Clamp(expiresInHours, 1, 24 * 30)),
            now);

        await familyInviteRepository.SaveChangesAsync(cancellationToken);
        await RecordTimelineEventAsync(
            invite,
            FamilyInviteTimelineEventType.Resent,
            actorUserId,
            now,
            cancellationToken);

        try
        {
            await familyInviteSender.SendInviteAsync(
                invite.FamilyId,
                invite.Email,
                invite.Role,
                inviteToken,
                invite.ExpiresAtUtc,
                cancellationToken);
        }
        catch
        {
            // Invite persistence should succeed even when outbound email fails.
        }

        return new CreateFamilyInviteResult(Map(invite), inviteToken);
    }

    public async Task<FamilyInviteDetails> AcceptAsync(
        string inviteToken,
        string? actorUserId = null,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = ComputeTokenHash(inviteToken);
        var invite = await familyInviteRepository.GetByTokenHashForUpdateAsync(tokenHash, cancellationToken)
            ?? throw new DomainValidationException("Invite was not found.");

        var now = clock.UtcNow;
        invite.Accept(now);
        await familyInviteRepository.SaveChangesAsync(cancellationToken);
        await RecordTimelineEventAsync(
            invite,
            FamilyInviteTimelineEventType.Accepted,
            actorUserId,
            now,
            cancellationToken);
        return Map(invite);
    }

    public async Task<FamilyInviteRedemptionDetails> RedeemAsync(
        string inviteToken,
        string keycloakUserId,
        string? memberName,
        string? memberEmail,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = ComputeTokenHash(inviteToken);
        var invite = await familyInviteRepository.GetByTokenHashForUpdateAsync(tokenHash, cancellationToken)
            ?? throw new DomainValidationException("Invite was not found.");

        var normalizedUserId = NormalizeRequired(keycloakUserId, "Authenticated user id");
        var now = clock.UtcNow;
        invite.Expire(now);

        var resolvedEmail = ResolveMemberEmail(memberEmail, invite.Email);
        if (!resolvedEmail.Equals(invite.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainValidationException("Invite email does not match the authenticated user email.");
        }

        var existingMembers = await familyRepository.ListMembersAsync(invite.FamilyId, cancellationToken);
        var existingMemberByUserId = existingMembers.FirstOrDefault(member =>
            member.KeycloakUserId.Equals(normalizedUserId, StringComparison.OrdinalIgnoreCase));

        if (existingMemberByUserId is not null)
        {
            if (invite.Status == FamilyInviteStatus.Pending)
            {
                invite.Accept(now);
                await familyInviteRepository.SaveChangesAsync(cancellationToken);
                await RecordTimelineEventAsync(
                    invite,
                    FamilyInviteTimelineEventType.Redeemed,
                    normalizedUserId,
                    now,
                    cancellationToken);
            }

            return new FamilyInviteRedemptionDetails(
                Map(invite),
                MapMember(existingMemberByUserId),
                CreatedNewMember: false);
        }

        if (existingMembers.Any(member => member.Email.Value.Equals(resolvedEmail, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainValidationException("A family member with this email already exists.");
        }

        EnsureRedeemable(invite);

        if (!Enum.TryParse<MemberRole>(invite.Role, ignoreCase: true, out var parsedRole))
        {
            throw new DomainValidationException("Invite role is invalid.");
        }

        var resolvedName = ResolveMemberName(memberName, resolvedEmail);
        var member = new FamilyMember(
            Guid.NewGuid(),
            invite.FamilyId,
            normalizedUserId,
            resolvedName,
            EmailAddress.Parse(resolvedEmail),
            parsedRole);

        await familyRepository.AddMemberAsync(member, cancellationToken);

        invite.Accept(now);
        await familyInviteRepository.SaveChangesAsync(cancellationToken);
        await RecordTimelineEventAsync(
            invite,
            FamilyInviteTimelineEventType.Redeemed,
            normalizedUserId,
            now,
            cancellationToken);

        return new FamilyInviteRedemptionDetails(
            Map(invite),
            MapMember(member),
            CreatedNewMember: true);
    }

    public async Task<IReadOnlyList<FamilyInviteTimelineEventDetails>> ListTimelineByFamilyAsync(
        Guid familyId,
        string? emailFilter = null,
        string? eventTypeFilter = null,
        int take = 200,
        CancellationToken cancellationToken = default)
    {
        var events = await familyInviteRepository.ListTimelineByFamilyAsync(familyId, cancellationToken);

        var filtered = events.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(emailFilter))
        {
            var normalizedEmailFilter = emailFilter.Trim().ToLowerInvariant();
            filtered = filtered.Where(timelineEvent => timelineEvent.Email.Contains(normalizedEmailFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(eventTypeFilter))
        {
            filtered = filtered.Where(timelineEvent =>
                timelineEvent.EventType.ToString().Equals(eventTypeFilter.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        var boundedTake = Math.Clamp(take, 1, 500);
        return filtered
            .OrderByDescending(static timelineEvent => timelineEvent.OccurredAtUtc)
            .Take(boundedTake)
            .Select(MapTimelineEvent)
            .ToArray();
    }

    private static string ComputeTokenHash(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new DomainValidationException("Invite token is required.");
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static FamilyInviteDetails Map(FamilyInvite invite)
    {
        return new FamilyInviteDetails(
            invite.Id,
            invite.FamilyId,
            invite.Email,
            invite.Role,
            invite.Status.ToString(),
            invite.CreatedAtUtc,
            invite.ExpiresAtUtc,
            invite.AcceptedAtUtc,
            invite.CancelledAtUtc);
    }

    private static FamilyMemberDetails MapMember(FamilyMember member)
    {
        return new FamilyMemberDetails(
            member.Id,
            member.FamilyId,
            member.KeycloakUserId,
            member.Name,
            member.Email.Value,
            member.Role.ToString());
    }

    private static FamilyInviteTimelineEventDetails MapTimelineEvent(FamilyInviteTimelineEvent timelineEvent)
    {
        return new FamilyInviteTimelineEventDetails(
            timelineEvent.Id,
            timelineEvent.FamilyId,
            timelineEvent.InviteId,
            timelineEvent.Email,
            timelineEvent.EventType.ToString(),
            timelineEvent.ActorUserId,
            timelineEvent.OccurredAtUtc);
    }

    private static void EnsureRedeemable(FamilyInvite invite)
    {
        if (invite.Status == FamilyInviteStatus.Pending)
        {
            return;
        }

        var message = invite.Status switch
        {
            FamilyInviteStatus.Accepted => "Invite was already redeemed.",
            FamilyInviteStatus.Cancelled => "Invite was cancelled.",
            FamilyInviteStatus.Expired => "Invite is expired.",
            _ => "Invite cannot be redeemed."
        };

        throw new DomainValidationException(message);
    }

    private static string ResolveMemberName(string? memberName, string resolvedEmail)
    {
        if (!string.IsNullOrWhiteSpace(memberName))
        {
            return memberName.Trim();
        }

        var atIndex = resolvedEmail.IndexOf('@');
        if (atIndex > 0)
        {
            return resolvedEmail[..atIndex];
        }

        return resolvedEmail;
    }

    private static string ResolveMemberEmail(string? memberEmail, string inviteEmail)
    {
        if (!string.IsNullOrWhiteSpace(memberEmail))
        {
            return EmailAddress.Parse(memberEmail).Value;
        }

        return inviteEmail;
    }

    private static string NormalizeRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private Task RecordTimelineEventAsync(
        FamilyInvite invite,
        FamilyInviteTimelineEventType eventType,
        string? actorUserId,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        var timelineEvent = new FamilyInviteTimelineEvent(
            Guid.NewGuid(),
            invite.FamilyId,
            invite.Id,
            invite.Email,
            eventType,
            NormalizeOptional(actorUserId),
            occurredAtUtc);
        return familyInviteRepository.AddTimelineEventAsync(timelineEvent, cancellationToken);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
