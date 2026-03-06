using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DragonEnvelopes.Application.Services;

public sealed class StripeWebhookService(
    IEnvelopePaymentCardRepository envelopePaymentCardRepository,
    IEnvelopeRepository envelopeRepository,
    IStripeWebhookEventRepository stripeWebhookEventRepository,
    IParentSpendNotificationService parentSpendNotificationService,
    IClock clock,
    IOptions<StripeWebhookOptions> stripeWebhookOptions,
    ILogger<StripeWebhookService> logger) : IStripeWebhookService
{
    private const string StripeProvider = "Stripe";

    public async Task<StripeWebhookProcessResult> ProcessAsync(
        string payload,
        string? stripeSignatureHeader,
        CancellationToken cancellationToken = default)
    {
        var options = stripeWebhookOptions.Value;
        if (!options.Enabled)
        {
            return StripeWebhookProcessResult.Disabled();
        }

        if (!VerifySignature(payload, stripeSignatureHeader, options))
        {
            return StripeWebhookProcessResult.InvalidSignature();
        }

        using var doc = JsonDocument.Parse(payload);
        if (!doc.RootElement.TryGetProperty("id", out var eventIdElement)
            || string.IsNullOrWhiteSpace(eventIdElement.GetString())
            || !doc.RootElement.TryGetProperty("type", out var eventTypeElement)
            || string.IsNullOrWhiteSpace(eventTypeElement.GetString()))
        {
            return StripeWebhookProcessResult.Failed("unknown", "unknown", "Webhook payload is missing event id/type.");
        }

        var eventId = eventIdElement.GetString()!;
        var eventType = eventTypeElement.GetString()!;

        var existing = await stripeWebhookEventRepository.GetByEventIdAsync(eventId, cancellationToken);
        if (existing is not null)
        {
            return StripeWebhookProcessResult.Duplicate(eventId, eventType);
        }

        var now = clock.UtcNow;
        Guid? familyId = null;
        Guid? envelopeId = null;
        Guid? cardId = null;
        var status = "Processed";
        string? error = null;
        StripeWebhookProcessResult result;

        try
        {
            var processed = await ProcessEventCoreAsync(
                eventId,
                eventType,
                doc.RootElement,
                cancellationToken);
            result = processed.Result;
            familyId = processed.FamilyId;
            envelopeId = processed.EnvelopeId;
            cardId = processed.CardId;
            if (result.Outcome.Equals("Ignored", StringComparison.OrdinalIgnoreCase))
            {
                status = "Ignored";
            }
            else if (result.Outcome.Equals("Processed", StringComparison.OrdinalIgnoreCase))
            {
                status = "Processed";
            }
            else
            {
                status = "Failed";
                error = result.Message;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stripe webhook processing failed. EventId={EventId}, EventType={EventType}", eventId, eventType);
            status = "Failed";
            error = ex.Message;
            result = StripeWebhookProcessResult.Failed(eventId, eventType, ex.Message);
        }

        await stripeWebhookEventRepository.AddAsync(
            new StripeWebhookEvent(
                Guid.NewGuid(),
                eventId,
                eventType,
                familyId,
                envelopeId,
                cardId,
                status,
                error,
                payload,
                now,
                clock.UtcNow),
            cancellationToken);
        await stripeWebhookEventRepository.SaveChangesAsync(cancellationToken);

        return result with { EventId = eventId, EventType = eventType };
    }

    private async Task<(StripeWebhookProcessResult Result, Guid? FamilyId, Guid? EnvelopeId, Guid? CardId)> ProcessEventCoreAsync(
        string eventId,
        string eventType,
        JsonElement root,
        CancellationToken cancellationToken)
    {
        if (!TryGetEventObject(root, out var eventObject))
        {
            return (StripeWebhookProcessResult.Ignored("unknown", eventType, "No data.object payload."), null, null, null);
        }

        var providerCardId = TryGetProviderCardId(eventObject);
        if (string.IsNullOrWhiteSpace(providerCardId))
        {
            return (StripeWebhookProcessResult.Ignored("unknown", eventType, "No provider card id on payload."), null, null, null);
        }

        var card = await envelopePaymentCardRepository.GetByProviderCardIdForUpdateAsync(
            StripeProvider,
            providerCardId,
            cancellationToken);
        if (card is null)
        {
            return (StripeWebhookProcessResult.Ignored("unknown", eventType, "Card not found."), null, null, null);
        }

        if (eventType.Equals("card_authorization", StringComparison.OrdinalIgnoreCase)
            || eventType.Equals("card_transaction", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryGetAmount(eventObject, out var amount))
            {
                return (
                    StripeWebhookProcessResult.Ignored("unknown", eventType, "Event does not contain amount."),
                    card.FamilyId,
                    card.EnvelopeId,
                    card.Id);
            }

            var envelope = await envelopeRepository.GetByIdForUpdateAsync(card.EnvelopeId, cancellationToken)
                ?? throw new DomainValidationException("Envelope was not found for Stripe webhook event.");
            var isCredit = IsCredit(eventObject, amount);
            var absoluteAmount = decimal.Abs(amount) / 100m;
            if (absoluteAmount <= 0m)
            {
                return (
                    StripeWebhookProcessResult.Ignored("unknown", eventType, "Amount was zero."),
                    card.FamilyId,
                    card.EnvelopeId,
                    card.Id);
            }

            if (isCredit)
            {
                envelope.Allocate(Money.FromDecimal(absoluteAmount), clock.UtcNow);
            }
            else
            {
                envelope.Spend(Money.FromDecimal(absoluteAmount), clock.UtcNow);
                var merchant = ResolveMerchant(eventObject);
                await parentSpendNotificationService.QueueSpendNotificationsAsync(
                    card.FamilyId,
                    card.EnvelopeId,
                    card.Id,
                    eventId,
                    absoluteAmount,
                    merchant,
                    envelope.CurrentBalance.Amount,
                    cancellationToken);
            }

            return (
                StripeWebhookProcessResult.Processed("unknown", eventType),
                card.FamilyId,
                card.EnvelopeId,
                card.Id);
        }

        if (eventType.Equals("balance_update", StringComparison.OrdinalIgnoreCase))
        {
            var status = TryGetString(eventObject, "status");
            if (!string.IsNullOrWhiteSpace(status))
            {
                card.ChangeStatus(status, clock.UtcNow);
            }

            return (
                StripeWebhookProcessResult.Processed("unknown", eventType),
                card.FamilyId,
                card.EnvelopeId,
                card.Id);
        }

        return (
            StripeWebhookProcessResult.Ignored("unknown", eventType, "Event type is not handled."),
            card.FamilyId,
            card.EnvelopeId,
            card.Id);
    }

    private bool VerifySignature(
        string payload,
        string? stripeSignatureHeader,
        StripeWebhookOptions options)
    {
        if (string.IsNullOrWhiteSpace(payload)
            || string.IsNullOrWhiteSpace(stripeSignatureHeader)
            || string.IsNullOrWhiteSpace(options.SigningSecret))
        {
            return false;
        }

        var components = stripeSignatureHeader
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
                && long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedTs))
            {
                timestamp = parsedTs;
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
                // no-op, continue checking any additional signatures in header
            }
        }

        return false;
    }

    private static bool TryGetEventObject(JsonElement root, out JsonElement eventObject)
    {
        eventObject = default;
        if (!root.TryGetProperty("data", out var dataElement)
            || dataElement.ValueKind != JsonValueKind.Object
            || !dataElement.TryGetProperty("object", out eventObject)
            || eventObject.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return true;
    }

    private static string? TryGetProviderCardId(JsonElement eventObject)
    {
        if (!eventObject.TryGetProperty("card", out var cardElement))
        {
            return null;
        }

        if (cardElement.ValueKind == JsonValueKind.String)
        {
            return cardElement.GetString();
        }

        if (cardElement.ValueKind == JsonValueKind.Object
            && cardElement.TryGetProperty("id", out var cardIdElement))
        {
            return cardIdElement.GetString();
        }

        return null;
    }

    private static bool TryGetAmount(JsonElement eventObject, out decimal amount)
    {
        amount = 0m;
        if (!eventObject.TryGetProperty("amount", out var amountElement))
        {
            return false;
        }

        if (amountElement.ValueKind == JsonValueKind.Number
            && amountElement.TryGetDecimal(out var parsed))
        {
            amount = parsed;
            return true;
        }

        return false;
    }

    private static bool IsCredit(JsonElement eventObject, decimal amount)
    {
        if (amount < 0m)
        {
            return true;
        }

        var direction = TryGetString(eventObject, "direction");
        if (direction.Equals("credit", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var type = TryGetString(eventObject, "type");
        return type.Equals("refund", StringComparison.OrdinalIgnoreCase);
    }

    private static string TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property)
               && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string ResolveMerchant(JsonElement eventObject)
    {
        var merchant = TryGetString(eventObject, "merchant");
        if (!string.IsNullOrWhiteSpace(merchant))
        {
            return merchant;
        }

        if (eventObject.TryGetProperty("merchant_data", out var merchantData)
            && merchantData.ValueKind == JsonValueKind.Object)
        {
            var name = TryGetString(merchantData, "name");
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return "Unknown Merchant";
    }
}
