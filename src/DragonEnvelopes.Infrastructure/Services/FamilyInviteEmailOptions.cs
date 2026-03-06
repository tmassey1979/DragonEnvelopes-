namespace DragonEnvelopes.Infrastructure.Services;

public sealed class FamilyInviteEmailOptions
{
    public bool Enabled { get; set; }
    public bool UseSmtp { get; set; }
    public string InviteBaseUrl { get; set; } = "http://localhost:5173";
    public string FromAddress { get; set; } = "noreply@dragonenvelopes.local";
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 25;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public bool SmtpEnableSsl { get; set; }
}
