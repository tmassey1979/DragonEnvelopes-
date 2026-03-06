namespace DragonEnvelopes.Application.Services;

public interface IFamilyInviteSender
{
    Task SendInviteAsync(
        Guid familyId,
        string email,
        string role,
        string inviteToken,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default);
}
