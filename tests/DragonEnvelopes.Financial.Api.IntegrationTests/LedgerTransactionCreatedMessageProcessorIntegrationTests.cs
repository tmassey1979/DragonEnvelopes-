using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Contracts.IntegrationEvents;
using DragonEnvelopes.Financial.Api.Services;
using DragonEnvelopes.Infrastructure.Persistence;
using DragonEnvelopes.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

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

    private static DragonEnvelopesDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DragonEnvelopesDbContext>()
            .UseInMemoryDatabase($"financial-inbox-{Guid.NewGuid():N}")
            .Options;
        return new DragonEnvelopesDbContext(options);
    }

    private static byte[] CreateEnvelopePayload(Guid eventId, Guid familyId)
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
            eventId.ToString("D"),
            LedgerIntegrationEventNames.TransactionCreated,
            "1.0",
            now,
            now,
            "ledger-api",
            Guid.NewGuid().ToString("D"),
            CausationId: null,
            familyId,
            payload);
        return IntegrationEventEnvelopeJson.SerializeToUtf8Bytes(envelope);
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
