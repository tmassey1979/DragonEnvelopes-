namespace DragonEnvelopes.Application.Services;

public interface IPlaidWebhookVerificationService
{
    PlaidWebhookVerificationResult Verify(string payload, string? plaidSignatureHeader);
}
