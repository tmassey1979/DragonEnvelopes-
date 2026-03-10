using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Contracts.IntegrationEvents;
using DragonEnvelopes.Financial.Api.Services;
using DragonEnvelopes.Infrastructure.Persistence;
using DragonEnvelopes.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace DragonEnvelopes.Financial.Api.IntegrationTests;

public sealed class LedgerTransactionCreatedMessageProcessorIntegrationTests
{
    [Fact]
    public async Task ProcessAsync_DuplicateDelivery_UsesInboxIdempotency()
    {
        await using var dbContext = CreateDbContext();
        var repository = new IntegrationInboxRepository(dbContext);
        var handler = new RecordingHandler();
        var processor = new LedgerTransactionCreatedMessageProcessor(
            repository,
            new IncrementingClock(),
            handler,
            NullLogger<LedgerTransactionCreatedMessageProcessor>.Instance);

        var eventId = Guid.NewGuid();
        var body = CreateEnvelopePayload(eventId, Guid.NewGuid());

        var first = await processor.ProcessAsync(
            body,
            IntegrationEventRoutingKeys.LedgerTransactionCreatedV1,
            maxRetryAttempts: 3);
        var second = await processor.ProcessAsync(
            body,
            IntegrationEventRoutingKeys.LedgerTransactionCreatedV1,
            maxRetryAttempts: 3);

        Assert.Equal(ConsumerMessageDisposition.Ack, first.Disposition);
        Assert.Equal(ConsumerMessageDisposition.Ack, second.Disposition);
        Assert.Equal(1, handler.InvocationCount);

        var inboxMessage = await dbContext.IntegrationInboxMessages.SingleAsync();
        Assert.Equal(eventId.ToString("D"), inboxMessage.EventId);
        Assert.Equal(1, inboxMessage.AttemptCount);
        Assert.True(inboxMessage.IsProcessed);
        Assert.False(inboxMessage.IsDeadLettered);
    }

    [Fact]
    public async Task ProcessAsync_RetryExhaustion_TransitionsToDeadLetter()
    {
        await using var dbContext = CreateDbContext();
        var repository = new IntegrationInboxRepository(dbContext);
        var handler = new RecordingHandler(failAlways: true);
        var processor = new LedgerTransactionCreatedMessageProcessor(
            repository,
            new IncrementingClock(),
            handler,
            NullLogger<LedgerTransactionCreatedMessageProcessor>.Instance);

        var body = CreateEnvelopePayload(Guid.NewGuid(), Guid.NewGuid());

        var first = await processor.ProcessAsync(
            body,
            IntegrationEventRoutingKeys.LedgerTransactionCreatedV1,
            maxRetryAttempts: 2);
        var second = await processor.ProcessAsync(
            body,
            IntegrationEventRoutingKeys.LedgerTransactionCreatedV1,
            maxRetryAttempts: 2);
        var third = await processor.ProcessAsync(
            body,
            IntegrationEventRoutingKeys.LedgerTransactionCreatedV1,
            maxRetryAttempts: 2);

        Assert.Equal(ConsumerMessageDisposition.Retry, first.Disposition);
        Assert.Equal(ConsumerMessageDisposition.DeadLetter, second.Disposition);
        Assert.Equal(ConsumerMessageDisposition.Ack, third.Disposition);
        Assert.Equal(2, handler.InvocationCount);

        var inboxMessage = await dbContext.IntegrationInboxMessages.SingleAsync();
        Assert.Equal(2, inboxMessage.AttemptCount);
        Assert.False(inboxMessage.IsProcessed);
        Assert.True(inboxMessage.IsDeadLettered);
        Assert.False(string.IsNullOrWhiteSpace(inboxMessage.LastError));
    }

    [Fact]
    public async Task ProcessAsync_EnvelopeMinorVersion_IsAccepted()
    {
        await using var dbContext = CreateDbContext();
        var repository = new IntegrationInboxRepository(dbContext);
        var handler = new RecordingHandler();
        var processor = new LedgerTransactionCreatedMessageProcessor(
            repository,
            new IncrementingClock(),
            handler,
            NullLogger<LedgerTransactionCreatedMessageProcessor>.Instance);

        var body = CreateEnvelopePayload(Guid.NewGuid(), Guid.NewGuid(), schemaVersion: "1.1");

        var result = await processor.ProcessAsync(
            body,
            IntegrationEventRoutingKeys.LedgerTransactionCreatedV1,
            maxRetryAttempts: 3);

        Assert.Equal(ConsumerMessageDisposition.Ack, result.Disposition);
        Assert.Equal(1, handler.InvocationCount);
    }

    [Fact]
    public async Task ProcessAsync_UnsupportedMajorVersion_DeadLettersMessage()
    {
        await using var dbContext = CreateDbContext();
        var repository = new IntegrationInboxRepository(dbContext);
        var handler = new RecordingHandler();
        var processor = new LedgerTransactionCreatedMessageProcessor(
            repository,
            new IncrementingClock(),
            handler,
            NullLogger<LedgerTransactionCreatedMessageProcessor>.Instance);

        var body = CreateEnvelopePayload(Guid.NewGuid(), Guid.NewGuid(), schemaVersion: "2.0");

        var result = await processor.ProcessAsync(
            body,
            IntegrationEventRoutingKeys.LedgerTransactionCreatedV1,
            maxRetryAttempts: 3);

        Assert.Equal(ConsumerMessageDisposition.DeadLetter, result.Disposition);
        Assert.Equal(0, handler.InvocationCount);
        Assert.Contains("Unsupported schema version", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        var inboxMessage = await dbContext.IntegrationInboxMessages.SingleAsync();
        Assert.True(inboxMessage.IsDeadLettered);
    }

    [Fact]
    public async Task ProcessAsync_LegacyRawPayload_RemainsCompatible()
    {
        await using var dbContext = CreateDbContext();
        var repository = new IntegrationInboxRepository(dbContext);
        var handler = new RecordingHandler();
        var processor = new LedgerTransactionCreatedMessageProcessor(
            repository,
            new IncrementingClock(),
            handler,
            NullLogger<LedgerTransactionCreatedMessageProcessor>.Instance);

        var body = CreateLegacyRawPayload(Guid.NewGuid(), Guid.NewGuid());

        var result = await processor.ProcessAsync(
            body,
            IntegrationEventRoutingKeys.LedgerTransactionCreatedV1,
            maxRetryAttempts: 3);

        Assert.Equal(ConsumerMessageDisposition.Ack, result.Disposition);
        Assert.Equal(1, handler.InvocationCount);

        var inboxMessage = await dbContext.IntegrationInboxMessages.SingleAsync();
        Assert.Equal("legacy-unknown", inboxMessage.SourceService);
        Assert.Equal("1.0", inboxMessage.SchemaVersion);
    }

    [Fact]
    public async Task ProcessAsync_InvalidEnvelopeMetadata_DeadLettersMessage()
    {
        await using var dbContext = CreateDbContext();
        var repository = new IntegrationInboxRepository(dbContext);
        var handler = new RecordingHandler();
        var processor = new LedgerTransactionCreatedMessageProcessor(
            repository,
            new IncrementingClock(),
            handler,
            NullLogger<LedgerTransactionCreatedMessageProcessor>.Instance);

        var body = CreateEnvelopePayload(
            eventId: Guid.NewGuid(),
            familyId: Guid.NewGuid(),
            schemaVersion: "1.0",
            eventName: "",
            sourceService: "",
            correlationId: "",
            envelopeEventId: "");

        var result = await processor.ProcessAsync(
            body,
            IntegrationEventRoutingKeys.LedgerTransactionCreatedV1,
            maxRetryAttempts: 3);

        Assert.Equal(ConsumerMessageDisposition.DeadLetter, result.Disposition);
        Assert.Equal(0, handler.InvocationCount);
        Assert.Contains("Invalid event envelope", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static DragonEnvelopesDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DragonEnvelopesDbContext>()
            .UseInMemoryDatabase($"financial-inbox-{Guid.NewGuid():N}")
            .Options;
        return new DragonEnvelopesDbContext(options);
    }

    private static byte[] CreateEnvelopePayload(
        Guid eventId,
        Guid familyId,
        string schemaVersion = "1.0",
        string? eventName = null,
        string? sourceService = "ledger-api",
        string? correlationId = null,
        string? envelopeEventId = null)
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new LedgerTransactionCreatedIntegrationEvent(
            eventId,
            now,
            familyId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            -27.32m,
            "Coffee run",
            "Dragon Cafe",
            "Food",
            EnvelopeId: null,
            IsSplit: false);
        var envelope = new IntegrationEventEnvelope<LedgerTransactionCreatedIntegrationEvent>(
            envelopeEventId ?? eventId.ToString("D"),
            eventName ?? LedgerIntegrationEventNames.TransactionCreated,
            schemaVersion,
            now,
            now,
            sourceService ?? "ledger-api",
            correlationId ?? Guid.NewGuid().ToString("D"),
            CausationId: null,
            familyId,
            payload);
        return IntegrationEventEnvelopeJson.SerializeToUtf8Bytes(envelope);
    }

    private static byte[] CreateLegacyRawPayload(Guid eventId, Guid familyId)
    {
        var payload = new LedgerTransactionCreatedIntegrationEvent(
            eventId,
            DateTimeOffset.UtcNow,
            familyId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            -15.12m,
            "Legacy raw payload",
            "Dragon Legacy",
            "Food",
            EnvelopeId: null,
            IsSplit: false);
        return JsonSerializer.SerializeToUtf8Bytes(payload);
    }

    private sealed class IncrementingClock : IClock
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;

        public DateTimeOffset UtcNow
        {
            get
            {
                _now = _now.AddSeconds(1);
                return _now;
            }
        }
    }

    private sealed class RecordingHandler : ILedgerTransactionCreatedEventHandler
    {
        private readonly bool _failAlways;

        public RecordingHandler(bool failAlways = false)
        {
            _failAlways = failAlways;
        }

        public int InvocationCount { get; private set; }

        public Task HandleAsync(
            LedgerTransactionCreatedIntegrationEvent payload,
            CancellationToken cancellationToken = default)
        {
            InvocationCount += 1;
            if (_failAlways)
            {
                throw new InvalidOperationException("Simulated processing failure.");
            }

            return Task.CompletedTask;
        }
    }
}
