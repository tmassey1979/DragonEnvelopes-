using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Application.Cqrs.Transactions;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public sealed class CqrsTransactionHandlersTests
{
    [Fact]
    public async Task CreateTransactionCommandHandler_Publishes_LedgerTransactionCreated_Event()
    {
        var accountId = Guid.Parse("90000000-0000-0000-0000-000000000001");
        var familyId = Guid.Parse("90000000-0000-0000-0000-000000000002");
        var transactionId = Guid.Parse("90000000-0000-0000-0000-000000000003");
        var occurredAt = DateTimeOffset.UtcNow.AddMinutes(-2);

        var transactionService = new Mock<ITransactionService>(MockBehavior.Strict);
        transactionService
            .Setup(service => service.CreateAsync(
                accountId,
                -42.50m,
                "Coffee",
                "Dragon Cafe",
                occurredAt,
                "Food",
                null,
                false,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionDetails(
                transactionId,
                accountId,
                -42.50m,
                "Coffee",
                "Dragon Cafe",
                occurredAt,
                "Food",
                null,
                null,
                null,
                null,
                []));

        var transactionRepository = new Mock<ITransactionRepository>(MockBehavior.Strict);
        transactionRepository
            .Setup(repository => repository.GetAccountFamilyIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(familyId);

        var publisher = new Mock<IIntegrationEventPublisher>(MockBehavior.Strict);
        LedgerTransactionCreatedIntegrationEvent? publishedEvent = null;
        publisher
            .Setup(p => p.PublishAsync(
                IntegrationEventRoutingKeys.LedgerTransactionCreatedV1,
                It.IsAny<LedgerTransactionCreatedIntegrationEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, LedgerTransactionCreatedIntegrationEvent, CancellationToken>((_, evt, _) => publishedEvent = evt)
            .Returns(Task.CompletedTask);

        var handler = new CreateTransactionCommandHandler(
            transactionService.Object,
            transactionRepository.Object,
            publisher.Object);
        var command = new CreateTransactionCommand(
            accountId,
            -42.50m,
            "Coffee",
            "Dragon Cafe",
            occurredAt,
            "Food",
            null,
            false,
            null);

        var result = await handler.HandleAsync(command);

        Assert.Equal(transactionId, result.Id);
        Assert.NotNull(publishedEvent);
        Assert.Equal(familyId, publishedEvent!.FamilyId);
        Assert.Equal(transactionId, publishedEvent.TransactionId);
        Assert.Equal(accountId, publishedEvent.AccountId);
        Assert.Equal(-42.50m, publishedEvent.Amount);
        Assert.False(publishedEvent.IsSplit);

        publisher.VerifyAll();
        transactionService.VerifyAll();
        transactionRepository.VerifyAll();
    }

    [Fact]
    public async Task ListTransactionsByAccountQueryHandler_Delegates_To_TransactionService()
    {
        var accountId = Guid.Parse("90000000-0000-0000-0000-000000000010");
        var expected = (IReadOnlyList<TransactionDetails>)
        [
            new TransactionDetails(
                Guid.Parse("90000000-0000-0000-0000-000000000011"),
                accountId,
                -12.00m,
                "Lunch",
                "Dragon Grill",
                DateTimeOffset.UtcNow,
                "Food",
                null,
                null,
                null,
                null,
                [])
        ];

        var transactionService = new Mock<ITransactionService>(MockBehavior.Strict);
        transactionService
            .Setup(service => service.ListAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new ListTransactionsByAccountQueryHandler(transactionService.Object);

        var result = await handler.HandleAsync(new ListTransactionsByAccountQuery(accountId));

        Assert.Single(result);
        Assert.Equal(expected[0].Id, result[0].Id);
        transactionService.VerifyAll();
    }
}
