using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Contracts.IntegrationEvents;
using DragonEnvelopes.Domain.Entities;

namespace DragonEnvelopes.Financial.Api.Services;

public sealed class LedgerTransactionCreatedMessageProcessor(
    IIntegrationInboxRepository integrationInboxRepository,
    IClock clock,
    ILedgerTransactionCreatedEventHandler handler,
    ILogger<LedgerTransactionCreatedMessageProcessor> logger)
{
    private const string ConsumerName = "financial.ledger-transaction-created-consumer";
    private const string LegacySourceService = "legacy-unknown";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<LedgerTransactionCreatedMessageProcessResult> ProcessAsync(
        ReadOnlyMemory<byte> messageBody,
        string routingKey,
        int maxRetryAttempts,
        CancellationToken cancellationToken = default)
    {
        var normalizedRoutingKey = NormalizeRequired(routingKey, "Routing key");
        var normalizedMaxRetryAttempts = Math.Max(1, maxRetryAttempts);
        if (!TryResolveMessage(messageBody.Span, normalizedRoutingKey, out var resolvedMessage, out var parseError))
        {
            return await DeadLetterPoisonMessageAsync(
                messageBody,
                normalizedRoutingKey,
                parseError,
                cancellationToken);
        }

        return await ProcessResolvedMessageAsync(
            resolvedMessage!,
            normalizedMaxRetryAttempts,
            cancellationToken);
    }

    private async Task<LedgerTransactionCreatedMessageProcessResult> ProcessResolvedMessageAsync(
        ResolvedLedgerMessage resolvedMessage,
        int maxRetryAttempts,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = BuildIdempotencyKey(
            ConsumerName,
            resolvedMessage.SourceService,
            resolvedMessage.EventId);
        var inboxMessage = await integrationInboxRepository.GetByIdempotencyKeyAsync(
            idempotencyKey,
            cancellationToken);
        if (inboxMessage is null)
        {
            inboxMessage = new IntegrationInboxMessage(
                Guid.NewGuid(),
                idempotencyKey,
                ConsumerName,
                resolvedMessage.SourceService,
                resolvedMessage.EventId,
                resolvedMessage.EventName,
                resolvedMessage.RoutingKey,
                resolvedMessage.SchemaVersion,
                resolvedMessage.FamilyId,
                resolvedMessage.PayloadJson,
                clock.UtcNow);
            await integrationInboxRepository.AddAsync(inboxMessage, cancellationToken);
        }

        if (inboxMessage.IsProcessed || inboxMessage.IsDeadLettered)
        {
            logger.LogDebug(
                "Skipping duplicate inbox event. IdempotencyKey={IdempotencyKey}, Processed={Processed}, DeadLettered={DeadLettered}",
                idempotencyKey,
                inboxMessage.IsProcessed,
                inboxMessage.IsDeadLettered);
            return new LedgerTransactionCreatedMessageProcessResult(
                ConsumerMessageDisposition.Ack,
                idempotencyKey,
                inboxMessage.AttemptCount,
                null);
        }

        inboxMessage.RegisterAttempt(clock.UtcNow);

        try
        {
            await handler.HandleAsync(resolvedMessage.Payload, cancellationToken);
            inboxMessage.MarkProcessed(clock.UtcNow);
            await integrationInboxRepository.SaveChangesAsync(cancellationToken);

            return new LedgerTransactionCreatedMessageProcessResult(
                ConsumerMessageDisposition.Ack,
                idempotencyKey,
                inboxMessage.AttemptCount,
                null);
        }
        catch (Exception ex)
        {
            var errorMessage = TruncateError(ex.Message);
            var attemptedAtUtc = clock.UtcNow;
            var isRetryExhausted = inboxMessage.AttemptCount >= maxRetryAttempts;
            if (isRetryExhausted)
            {
                inboxMessage.MarkDeadLettered(errorMessage, attemptedAtUtc);
            }
            else
            {
                inboxMessage.MarkRetry(errorMessage, attemptedAtUtc);
            }

            await integrationInboxRepository.SaveChangesAsync(cancellationToken);
            logger.LogWarning(
                ex,
                "Ledger transaction event processing failed. EventId={EventId}, IdempotencyKey={IdempotencyKey}, AttemptCount={AttemptCount}, RetryExhausted={RetryExhausted}",
                resolvedMessage.EventId,
                idempotencyKey,
                inboxMessage.AttemptCount,
                isRetryExhausted);

            return new LedgerTransactionCreatedMessageProcessResult(
                isRetryExhausted
                    ? ConsumerMessageDisposition.DeadLetter
                    : ConsumerMessageDisposition.Retry,
                idempotencyKey,
                inboxMessage.AttemptCount,
                errorMessage);
        }
    }

    private async Task<LedgerTransactionCreatedMessageProcessResult> DeadLetterPoisonMessageAsync(
        ReadOnlyMemory<byte> messageBody,
        string routingKey,
        string parseError,
        CancellationToken cancellationToken)
    {
        var messageHash = Convert.ToHexString(SHA256.HashData(messageBody.Span)).ToLowerInvariant();
        var idempotencyKey = BuildIdempotencyKey(
            ConsumerName,
            LegacySourceService,
            messageHash);
        var inboxMessage = await integrationInboxRepository.GetByIdempotencyKeyAsync(
            idempotencyKey,
            cancellationToken);
        if (inboxMessage is null)
        {
            inboxMessage = new IntegrationInboxMessage(
                Guid.NewGuid(),
                idempotencyKey,
                ConsumerName,
                LegacySourceService,
                messageHash,
                LedgerIntegrationEventNames.TransactionCreated,
                routingKey,
                "unknown",
                familyId: null,
                ResolveRawPayloadJson(messageBody),
                clock.UtcNow);
            await integrationInboxRepository.AddAsync(inboxMessage, cancellationToken);
        }

        if (!inboxMessage.IsProcessed && !inboxMessage.IsDeadLettered)
        {
            var attemptedAtUtc = clock.UtcNow;
            var errorMessage = TruncateError(parseError);
            inboxMessage.RegisterAttempt(attemptedAtUtc);
            inboxMessage.MarkDeadLettered(errorMessage, attemptedAtUtc);
            await integrationInboxRepository.SaveChangesAsync(cancellationToken);
        }

        logger.LogWarning(
            "Dead-lettering poison ledger transaction message. IdempotencyKey={IdempotencyKey}, Reason={Reason}",
            idempotencyKey,
            parseError);

        return new LedgerTransactionCreatedMessageProcessResult(
            ConsumerMessageDisposition.DeadLetter,
            idempotencyKey,
            inboxMessage.AttemptCount,
            TruncateError(parseError));
    }

    private static bool TryResolveMessage(
        ReadOnlySpan<byte> bodySpan,
        string routingKey,
        out ResolvedLedgerMessage? resolvedMessage,
        out string parseError)
    {
        var looksLikeEnvelope = false;
        try
        {
            using var document = JsonDocument.Parse(bodySpan.ToArray());
            looksLikeEnvelope = document.RootElement.ValueKind == JsonValueKind.Object
                                && document.RootElement.TryGetProperty("payload", out _);
        }
        catch (JsonException ex)
        {
            parseError = $"Unable to parse message body: {ex.Message}";
            resolvedMessage = null;
            return false;
        }

        try
        {
            if (looksLikeEnvelope)
            {
                var envelope = IntegrationEventEnvelopeJson.Deserialize<LedgerTransactionCreatedIntegrationEvent>(bodySpan);
                if (envelope is null)
                {
                    parseError = "Event envelope payload is empty.";
                    resolvedMessage = null;
                    return false;
                }

                if (!IntegrationEventEnvelopeValidator.TryValidate(envelope, out var validationErrors))
                {
                    parseError = $"Invalid event envelope: {string.Join("; ", validationErrors)}";
                    resolvedMessage = null;
                    return false;
                }

                if (!IntegrationEventEnvelopeValidator.IsSupportedMajorVersion(
                        envelope.SchemaVersion,
                        supportedMajorVersion: 1))
                {
                    parseError = $"Unsupported schema version '{envelope.SchemaVersion}'.";
                    resolvedMessage = null;
                    return false;
                }

                resolvedMessage = new ResolvedLedgerMessage(
                    envelope.EventId.Trim(),
                    envelope.EventName.Trim(),
                    envelope.SchemaVersion.Trim(),
                    envelope.SourceService.Trim(),
                    envelope.FamilyId,
                    routingKey,
                    JsonSerializer.Serialize(envelope.Payload, SerializerOptions),
                    envelope.Payload);
                parseError = string.Empty;
                return true;
            }
        }
        catch (JsonException)
        {
            if (looksLikeEnvelope)
            {
                parseError = "Unable to deserialize integration event envelope payload.";
                resolvedMessage = null;
                return false;
            }
        }

        try
        {
            var payload = JsonSerializer.Deserialize<LedgerTransactionCreatedIntegrationEvent>(
                bodySpan,
                SerializerOptions);
            if (payload is null)
            {
                parseError = "Ledger transaction payload is empty.";
                resolvedMessage = null;
                return false;
            }

            resolvedMessage = new ResolvedLedgerMessage(
                payload.EventId.ToString("D"),
                LedgerIntegrationEventNames.TransactionCreated,
                "1.0",
                LegacySourceService,
                payload.FamilyId,
                routingKey,
                JsonSerializer.Serialize(payload, SerializerOptions),
                payload);
            parseError = string.Empty;
            return true;
        }
        catch (JsonException ex)
        {
            parseError = $"Unable to deserialize ledger transaction payload: {ex.Message}";
            resolvedMessage = null;
            return false;
        }
    }

    private static string ResolveRawPayloadJson(ReadOnlyMemory<byte> messageBody)
    {
        try
        {
            return Encoding.UTF8.GetString(messageBody.Span);
        }
        catch (Exception)
        {
            return Convert.ToBase64String(messageBody.ToArray());
        }
    }

    private static string BuildIdempotencyKey(
        string consumerName,
        string sourceService,
        string eventId)
    {
        return $"{NormalizeSegment(consumerName)}:{NormalizeSegment(sourceService)}:{NormalizeSegment(eventId)}";
    }

    private static string NormalizeSegment(string value)
    {
        return NormalizeRequired(value, "Idempotency segment")
            .ToLowerInvariant();
    }

    private static string NormalizeRequired(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{field} is required.");
        }

        return value.Trim();
    }

    private static string TruncateError(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return "Unknown processing failure.";
        }

        var trimmed = errorMessage.Trim();
        return trimmed.Length <= 1900
            ? trimmed
            : trimmed[..1900];
    }

    private sealed record ResolvedLedgerMessage(
        string EventId,
        string EventName,
        string SchemaVersion,
        string SourceService,
        Guid? FamilyId,
        string RoutingKey,
        string PayloadJson,
        LedgerTransactionCreatedIntegrationEvent Payload);
}
