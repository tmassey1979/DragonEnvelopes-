namespace DragonEnvelopes.Application.Services;

public sealed class PlaidWebhookVerificationOptions
{
    public bool Enabled { get; set; } = true;
    public bool AllowUnsignedInDevelopment { get; set; }
    public string SigningSecret { get; set; } = string.Empty;
    public int SignatureToleranceSeconds { get; set; } = 300;
}

public sealed record PlaidWebhookVerificationResult(
    bool IsVerified,
    bool IsDisabled,
    string Message)
{
    public static PlaidWebhookVerificationResult Verified() =>
        new(true, false, "Plaid webhook signature verification succeeded.");

    public static PlaidWebhookVerificationResult Disabled() =>
        new(false, true, "Plaid webhook signature verification is disabled.");

    public static PlaidWebhookVerificationResult DevelopmentBypass() =>
        new(true, false, "Plaid webhook signature verification bypassed for development.");

    public static PlaidWebhookVerificationResult Invalid(string message) =>
        new(false, false, message);
}
