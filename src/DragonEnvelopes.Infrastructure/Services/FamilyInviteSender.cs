using System.Net;
using System.Net.Mail;
using DragonEnvelopes.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DragonEnvelopes.Infrastructure.Services;

public sealed class FamilyInviteSender(
    IOptions<FamilyInviteEmailOptions> options,
    ILogger<FamilyInviteSender> logger) : IFamilyInviteSender
{
    public async Task SendInviteAsync(
        Guid familyId,
        string email,
        string role,
        string inviteToken,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var configured = options.Value;
        var inviteLink = BuildInviteLink(configured.InviteBaseUrl, inviteToken);
        var subject = "DragonEnvelopes family invite";
        var body = $"You have been invited to join a DragonEnvelopes family as {role}.{Environment.NewLine}"
                   + $"Invite link: {inviteLink}{Environment.NewLine}"
                   + $"Invite token: {inviteToken}{Environment.NewLine}"
                   + $"Expires: {expiresAtUtc:yyyy-MM-dd HH:mm:ss} UTC";

        if (configured.Enabled && configured.UseSmtp && !string.IsNullOrWhiteSpace(configured.SmtpHost))
        {
            try
            {
                using var smtpClient = new SmtpClient(configured.SmtpHost, configured.SmtpPort)
                {
                    EnableSsl = configured.SmtpEnableSsl
                };

                if (!string.IsNullOrWhiteSpace(configured.SmtpUsername))
                {
                    smtpClient.Credentials = new NetworkCredential(
                        configured.SmtpUsername,
                        configured.SmtpPassword ?? string.Empty);
                }

                using var mailMessage = new MailMessage(
                    configured.FromAddress,
                    email,
                    subject,
                    body);

                await smtpClient.SendMailAsync(mailMessage, cancellationToken);
                logger.LogInformation(
                    "Family invite email sent. FamilyId={FamilyId}, Email={Email}, Role={Role}, ExpiresAtUtc={ExpiresAtUtc}",
                    familyId,
                    email,
                    role,
                    expiresAtUtc);
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Family invite email send failed; falling back to dev log output. FamilyId={FamilyId}, Email={Email}",
                    familyId,
                    email);
            }
        }

        logger.LogInformation(
            "Family invite email fallback output. FamilyId={FamilyId}, Email={Email}, Role={Role}, InviteLink={InviteLink}, InviteToken={InviteToken}, ExpiresAtUtc={ExpiresAtUtc}",
            familyId,
            email,
            role,
            inviteLink,
            inviteToken,
            expiresAtUtc);
    }

    private static string BuildInviteLink(string baseUrl, string token)
    {
        var trimmedBaseUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? "http://localhost:5173"
            : baseUrl.TrimEnd('/');
        return $"{trimmedBaseUrl}/invite/accept?token={Uri.EscapeDataString(token)}";
    }
}
