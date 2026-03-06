using System.Security.Cryptography;
using System.Text;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Application.Services;

public sealed class FamilyInviteService(
    IFamilyRepository familyRepository,
    IFamilyInviteRepository familyInviteRepository,
    IClock clock) : IFamilyInviteService
{
    private static readonly string[] AllowedRoles = ["Parent", "Adult", "Teen", "Child"];

    public async Task<CreateFamilyInviteResult> CreateAsync(
        Guid familyId,
        string email,
        string role,
        int expiresInHours,
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
        CancellationToken cancellationToken = default)
    {
        var invite = await familyInviteRepository.GetByIdForUpdateAsync(inviteId, cancellationToken)
            ?? throw new DomainValidationException("Invite was not found.");

        invite.Expire(clock.UtcNow);
        invite.Cancel(clock.UtcNow);
        await familyInviteRepository.SaveChangesAsync(cancellationToken);
        return Map(invite);
    }

    public async Task<FamilyInviteDetails> AcceptAsync(
        string inviteToken,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = ComputeTokenHash(inviteToken);
        var invite = await familyInviteRepository.GetByTokenHashForUpdateAsync(tokenHash, cancellationToken)
            ?? throw new DomainValidationException("Invite was not found.");

        invite.Accept(clock.UtcNow);
        await familyInviteRepository.SaveChangesAsync(cancellationToken);
        return Map(invite);
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
}
