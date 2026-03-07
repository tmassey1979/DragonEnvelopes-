using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using DragonEnvelopes.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DragonEnvelopes.Family.Api.IntegrationTests;

public sealed class FamilyOutboxIntegrationTests
{
    [Fact]
    public async Task Family_Create_Persists_Family_And_Outbox_Message()
    {
        await using var dbContext = CreateDbContext();
        var clock = new MutableClock(DateTimeOffset.Parse("2026-03-07T12:00:00+00:00"));
        var familyRepository = new FamilyRepository(dbContext);
        var outboxRepository = new IntegrationOutboxRepository(dbContext);
        var familyService = new FamilyService(familyRepository, outboxRepository, clock);

        var created = await familyService.CreateAsync("Outbox Integration Family");

        var persistedFamily = await dbContext.Families
            .AsNoTracking()
            .SingleOrDefaultAsync(family => family.Id == created.Id);
        var outboxMessages = await dbContext.IntegrationOutboxMessages
            .AsNoTracking()
            .Where(message => message.FamilyId == created.Id)
            .ToArrayAsync();

        Assert.NotNull(persistedFamily);
        Assert.Single(outboxMessages);
        Assert.Equal(FamilyIntegrationEventNames.FamilyCreated, outboxMessages[0].EventName);
        Assert.Equal(IntegrationEventRoutingKeys.FamilyCreatedV1, outboxMessages[0].RoutingKey);
        Assert.Null(outboxMessages[0].DispatchedAtUtc);
    }

    [Fact]
    public async Task Outbox_Dispatch_Retries_Failure_Then_Dispatches()
    {
        await using var dbContext = CreateDbContext();
        var clock = new MutableClock(DateTimeOffset.Parse("2026-03-07T13:00:00+00:00"));
        var outboxRepository = new IntegrationOutboxRepository(dbContext);

        var message = new IntegrationOutboxMessage(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid().ToString("D"),
            IntegrationEventRoutingKeys.FamilyMemberAddedV1,
            FamilyIntegrationEventNames.FamilyMemberAdded,
            "1.0",
            "family-api",
            Guid.NewGuid().ToString("D"),
            causationId: null,
            "{\"memberId\":\"test\"}",
            clock.UtcNow,
            clock.UtcNow);
        await outboxRepository.AddAsync(message);
        await outboxRepository.SaveChangesAsync();

        var publisher = new FlakyOutboxPublisher(failuresBeforeSuccess: 1);
        var dispatchService = new IntegrationOutboxDispatchService(
            outboxRepository,
            publisher,
            clock,
            NullLogger<IntegrationOutboxDispatchService>.Instance);

        var firstResult = await dispatchService.DispatchPendingAsync(20);
        Assert.Equal(1, firstResult.LoadedCount);
        Assert.Equal(0, firstResult.PublishedCount);
        Assert.Equal(1, firstResult.FailedCount);

        clock.Advance(TimeSpan.FromSeconds(6));
        var secondResult = await dispatchService.DispatchPendingAsync(20);
        Assert.Equal(1, secondResult.LoadedCount);
        Assert.Equal(1, secondResult.PublishedCount);
        Assert.Equal(0, secondResult.FailedCount);

        var persisted = await dbContext.IntegrationOutboxMessages
            .AsNoTracking()
            .SingleAsync(row => row.Id == message.Id);
        Assert.Equal(1, persisted.AttemptCount);
        Assert.NotNull(persisted.DispatchedAtUtc);
        Assert.Equal(2, publisher.PublishCallCount);
    }

    private static DragonEnvelopesDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DragonEnvelopesDbContext>()
            .UseInMemoryDatabase($"family-outbox-integration-{Guid.NewGuid()}")
            .Options;
        return new DragonEnvelopesDbContext(options);
    }

    private sealed class FlakyOutboxPublisher(int failuresBeforeSuccess) : IIntegrationOutboxMessagePublisher
    {
        private int _remainingFailures = Math.Max(0, failuresBeforeSuccess);

        public int PublishCallCount { get; private set; }

        public Task PublishAsync(IntegrationOutboxEnvelopeMessage message, CancellationToken cancellationToken = default)
        {
            PublishCallCount += 1;
            if (_remainingFailures > 0)
            {
                _remainingFailures -= 1;
                throw new InvalidOperationException("Simulated RabbitMQ outage.");
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
