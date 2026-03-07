using DragonEnvelopes.Application.Cqrs.Accounts;
using DragonEnvelopes.Application.Cqrs.Imports;
using DragonEnvelopes.Application.Cqrs.Transactions;
using DragonEnvelopes.Application.Cqrs.Transfers;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public sealed class CqrsLedgerHandlersTests
{
    [Fact]
    public async Task AccountCommandAndQueryHandlers_Delegate_To_Service()
    {
        var service = new Mock<IAccountService>(MockBehavior.Strict);
        var familyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var created = new AccountDetails(accountId, familyId, "Checking", "Checking", 123.45m);
        var listed = (IReadOnlyList<AccountDetails>)[created];

        service.Setup(x => x.CreateAsync(familyId, "Checking", "Checking", 123.45m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);
        service.Setup(x => x.ListAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listed);

        var createHandler = new CreateAccountCommandHandler(service.Object);
        var listHandler = new ListAccountsQueryHandler(service.Object);

        var createResult = await createHandler.HandleAsync(
            new CreateAccountCommand(familyId, "Checking", "Checking", 123.45m));
        var listResult = await listHandler.HandleAsync(new ListAccountsQuery(familyId));

        Assert.Equal(accountId, createResult.Id);
        Assert.Single(listResult);
        service.VerifyAll();
    }

    [Fact]
    public async Task ImportCommandAndQueryHandlers_Delegate_To_Service()
    {
        var service = new Mock<IImportService>(MockBehavior.Strict);
        var familyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var preview = new ImportPreviewDetails(2, 2, 0, []);
        var commit = new ImportCommitDetails(2, 2, 0, 2, 0);

        service.Setup(x => x.PreviewTransactionsAsync(
                familyId,
                accountId,
                "csv",
                ",",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(preview);
        service.Setup(x => x.CommitTransactionsAsync(
                familyId,
                accountId,
                "csv",
                ",",
                null,
                It.Is<IReadOnlyList<int>?>(rows => rows != null && rows.Count == 2 && rows[0] == 1 && rows[1] == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(commit);

        var previewHandler = new PreviewTransactionImportQueryHandler(service.Object);
        var commitHandler = new CommitTransactionImportCommandHandler(service.Object);

        var previewResult = await previewHandler.HandleAsync(
            new PreviewTransactionImportQuery(familyId, accountId, "csv", ",", null));
        var commitResult = await commitHandler.HandleAsync(
            new CommitTransactionImportCommand(familyId, accountId, "csv", ",", null, [1, 2]));

        Assert.Equal(2, previewResult.Parsed);
        Assert.Equal(2, commitResult.Inserted);
        service.VerifyAll();
    }

    [Fact]
    public async Task CreateEnvelopeTransferCommandHandler_Delegates_To_Service()
    {
        var service = new Mock<IEnvelopeTransferService>(MockBehavior.Strict);
        var familyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var fromEnvelopeId = Guid.NewGuid();
        var toEnvelopeId = Guid.NewGuid();
        var transfer = new EnvelopeTransferDetails(
            Guid.NewGuid(),
            familyId,
            accountId,
            fromEnvelopeId,
            toEnvelopeId,
            20m,
            DateTimeOffset.UtcNow,
            "move",
            Guid.NewGuid(),
            Guid.NewGuid());

        service.Setup(x => x.CreateAsync(
                familyId,
                accountId,
                fromEnvelopeId,
                toEnvelopeId,
                20m,
                transfer.OccurredAt,
                "move",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transfer);

        var handler = new CreateEnvelopeTransferCommandHandler(service.Object);
        var result = await handler.HandleAsync(
            new CreateEnvelopeTransferCommand(
                familyId,
                accountId,
                fromEnvelopeId,
                toEnvelopeId,
                20m,
                transfer.OccurredAt,
                "move"));

        Assert.Equal(transfer.TransferId, result.TransferId);
        service.VerifyAll();
    }

    [Fact]
    public async Task TransactionMutationCommandHandlers_Delegate_To_Service()
    {
        var service = new Mock<ITransactionService>(MockBehavior.Strict);
        var transactionId = Guid.NewGuid();
        var updated = new TransactionDetails(
            transactionId,
            Guid.NewGuid(),
            -12.5m,
            "Updated",
            "Merchant",
            DateTimeOffset.UtcNow,
            "Food",
            null,
            null,
            null,
            null,
            []);
        var restored = updated with { Description = "Restored" };

        service.Setup(x => x.UpdateAsync(
                transactionId,
                "Updated",
                "Merchant",
                "Food",
                false,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);
        service.Setup(x => x.DeleteAsync(transactionId, "user-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        service.Setup(x => x.RestoreAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(restored);

        var updateHandler = new UpdateTransactionCommandHandler(service.Object);
        var deleteHandler = new DeleteTransactionCommandHandler(service.Object);
        var restoreHandler = new RestoreTransactionCommandHandler(service.Object);

        var updateResult = await updateHandler.HandleAsync(
            new UpdateTransactionCommand(transactionId, "Updated", "Merchant", "Food", false, null, null));
        var deleteResult = await deleteHandler.HandleAsync(new DeleteTransactionCommand(transactionId, "user-1"));
        var restoreResult = await restoreHandler.HandleAsync(new RestoreTransactionCommand(transactionId));

        Assert.Equal(updated.Id, updateResult.Id);
        Assert.True(deleteResult);
        Assert.Equal("Restored", restoreResult.Description);
        service.VerifyAll();
    }

    [Fact]
    public async Task ListDeletedTransactionsQueryHandler_Delegates_To_Service()
    {
        var service = new Mock<ITransactionService>(MockBehavior.Strict);
        var familyId = Guid.NewGuid();
        var expected = (IReadOnlyList<TransactionDetails>)
        [
            new TransactionDetails(
                Guid.NewGuid(),
                Guid.NewGuid(),
                -25m,
                "Deleted",
                "Merchant",
                DateTimeOffset.UtcNow,
                "Misc",
                null,
                null,
                null,
                null,
                [],
                DateTimeOffset.UtcNow,
                "user-1")
        ];

        service.Setup(x => x.ListDeletedAsync(familyId, 45, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new ListDeletedTransactionsQueryHandler(service.Object);
        var result = await handler.HandleAsync(new ListDeletedTransactionsQuery(familyId, 45));

        Assert.Single(result);
        Assert.Equal(expected[0].Id, result[0].Id);
        service.VerifyAll();
    }
}
