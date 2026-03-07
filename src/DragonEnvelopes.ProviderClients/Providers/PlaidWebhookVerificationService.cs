using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DragonEnvelopes.ProviderClients.Providers;

public sealed class PlaidWebhookVerificationService(
    IOptions<PlaidWebhookVerificationOptions> plaidWebhookVerificationOptions,
    IClock clock,
    ILogger<PlaidWebhookVerificationService> logger) : IPlaidWebhookVerificationService
{
    public PlaidWebhookVerificationResult Verify(string payload, string? plaidSignatureHeader)
    {
        var options = plaidWebhookVerificationOptions.Value;
        if (!options.Enabled)
        {
            return PlaidWebhookVerificationResult.Disabled();
        }

        if (options.AllowUnsignedInDevelopment)
        {
            return PlaidWebhookVerificationResult.DevelopmentBypass();
        }

        if (string.IsNullOrWhiteSpace(options.SigningSecret))
        {
            logger.LogWarning("Plaid webhook verification failed because signing secret is not configured.");
            return PlaidWebhookVerificationResult.Invalid("Plaid webhook verification is not configured.");
        }

        if (!VerifySignature(payload, plaidSignatureHeader, options))
        {
            return PlaidWebhookVerificationResult.Invalid("Plaid signature verification failed.");
        }

        return PlaidWebhookVerificationResult.Verified();
    }

    private bool VerifySignature(
        string payload,
        string? plaidSignatureHeader,
        PlaidWebhookVerificationOptions options)
    {
        if (string.IsNullOrWhiteSpace(payload)
            || string.IsNullOrWhiteSpace(plaidSignatureHeader))
        {
            return false;
        }

        var components = plaidSignatureHeader
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var timestamp = default(long?);
        var signatures = new List<string>();
        foreach (var component in components)
        {
            var parts = component.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                continue;
            }

            if (parts[0].Equals("t", StringComparison.OrdinalIgnoreCase)
                && long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedTimestamp))
            {
                timestamp = parsedTimestamp;
            }
            else if (parts[0].Equals("v1", StringComparison.OrdinalIgnoreCase))
            {
                signatures.Add(parts[1]);
            }
        }

        if (!timestamp.HasValue || signatures.Count == 0)
        {
            return false;
        }

        var nowUnix = clock.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(nowUnix - timestamp.Value) > options.SignatureToleranceSeconds)
        {
            return false;
        }

        var signedPayload = $"{timestamp.Value}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(options.SigningSecret));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();
        var expectedBytes = Convert.FromHexString(expected);

        foreach (var signature in signatures)
        {
            try
            {
                var actualBytes = Convert.FromHexString(signature);
                if (CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes))
                {
                    return true;
                }
            }
            catch (FormatException)
            {
                // Continue checking additional signatures, if any.
            }
        }

        return false;
    }
}
