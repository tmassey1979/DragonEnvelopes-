using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using DragonEnvelopes.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DragonEnvelopes.Ledger.Api.IntegrationTests;

public sealed class LedgerOutboxDispatchIntegrationTests
{
    [Fact]
    public async Task Ledger_Outbox_Dispatch_Retries_And_Does_Not_Dispatch_Other_Service_Messages()
    {
        await using var dbContext = CreateDbContext();
        var clock = new MutableClock(DateTimeOffset.Parse("2026-03-07T18:00:00+00:00"));
        var repository = new IntegrationOutboxRepository(dbContext);

        var ledgerMessage = CreateMessage(sourceService: "ledger-api", clock.UtcNow);
        var familyMessage = CreateMessage(sourceService: "family-api", clock.UtcNow);
        await repository.AddAsync(ledgerMessage);
        await repository.AddAsync(familyMessage);
        await repository.SaveChangesAsync();

        var publisher = new FlakyPublisher(failuresBeforeSuccess: 1);
        var service = new IntegrationOutboxDispatchService(
            repository,
            publisher,
            clock,
            NullLogger<IntegrationOutboxDispatchService>.Instance);

        var first = await service.DispatchPendingAsync("ledger-api", 20);
        Assert.Equal(1, first.LoadedCount);
        Assert.Equal(0, first.PublishedCount);
        Assert.Equal(1, first.FailedCount);

        clock.Advance(TimeSpan.FromSeconds(6));
        var second = await service.DispatchPendingAsync("ledger-api", 20);
        Assert.Equal(1, second.LoadedCount);
        Assert.Equal(1, second.PublishedCount);
        Assert.Equal(0, second.FailedCount);

        var persistedLedger = await dbContext.IntegrationOutboxMessages
            .AsNoTracking()
            .SingleAsync(message => message.Id == ledgerMessage.Id);
        var persistedFamily = await dbContext.IntegrationOutboxMessages
            .AsNoTracking()
            .SingleAsync(message => message.Id == familyMessage.Id);

        Assert.Equal(1, persistedLedger.AttemptCount);
        Assert.NotNull(persistedLedger.DispatchedAtUtc);
        Assert.Null(persistedFamily.DispatchedAtUtc);
        Assert.Equal(2, publisher.PublishCallCount);
    }

    private static IntegrationOutboxMessage CreateMessage(string sourceService, DateTimeOffset nowUtc)
    {
        return new IntegrationOutboxMessage(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid().ToString("D"),
            "ledger.transaction.created.v1",
            "TransactionCreated",
            "1.0",
            sourceService,
            Guid.NewGuid().ToString("D"),
            causationId: null,
            "{\"transactionId\":\"abc\"}",
            nowUtc,
            nowUtc);
    }

    private static DragonEnvelopesDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DragonEnvelopesDbContext>()
            .UseInMemoryDatabase($"ledger-outbox-dispatch-{Guid.NewGuid()}")
            .Options;
        return new DragonEnvelopesDbContext(options);
    }

    private sealed class FlakyPublisher(int failuresBeforeSuccess) : IIntegrationOutboxMessagePublisher
    {
        private int _remainingFailures = Math.Max(0, failuresBeforeSuccess);
        public int PublishCallCount { get; private set; }

        public Task PublishAsync(IntegrationOutboxEnvelopeMessage message, CancellationToken cancellationToken = default)
        {
            PublishCallCount += 1;
            if (_remainingFailures > 0)
            {
                _remainingFailures -= 1;
                throw new InvalidOperationException("Simulated broker outage.");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class MutableClock(DateTimeOffset initialUtcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; private set; } = initialUtcNow;

        public void Advance(TimeSpan duration)
        {
            UtcNow = UtcNow.Add(duration);
        }
    }
}
