using DragonEnvelopes.Desktop.Services;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Tests;

public sealed class TransactionsViewModelTests
{
    [Fact]
    public async Task Submit_WhenEditingAndPreservingAllocation_CallsUpdateWithoutAllocationPayload()
    {
        var accountId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var envelopeId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var transactionId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var dataService = new FakeTransactionsDataService(accountId, envelopeId, transactionId);
        var viewModel = new TransactionsViewModel(dataService);
        await viewModel.LoadAccountsCommand.ExecuteAsync(null);

        viewModel.SelectedTransaction = viewModel.Transactions.Single();
        viewModel.BeginEditSelectedTransactionCommand.Execute(null);
        viewModel.DraftMerchant = "Updated Merchant";
        viewModel.DraftDescription = "Updated Description";
        viewModel.DraftCategory = "Updated Category";
        viewModel.ReplaceAllocationOnEdit = false;
        viewModel.UseSplitEditor = false;

        await viewModel.SubmitTransactionCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasError);
        Assert.Equal("Transaction updated.", viewModel.EditorStatusMessage);
        Assert.False(viewModel.IsEditMode);
        Assert.Equal("Create", viewModel.SubmitButtonText);
        Assert.Single(dataService.UpdateCalls);
        var update = dataService.UpdateCalls[0];
        Assert.Equal(transactionId, update.TransactionId);
        Assert.False(update.ReplaceAllocation);
        Assert.Null(update.EnvelopeId);
        Assert.Null(update.Splits);
    }

    [Fact]
    public async Task Submit_WhenEditingWithSplitEditor_CallsUpdateWithAllocationReplacement()
    {
        var accountId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var envelopeId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var transactionId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var dataService = new FakeTransactionsDataService(accountId, envelopeId, transactionId);
        var viewModel = new TransactionsViewModel(dataService);
        await viewModel.LoadAccountsCommand.ExecuteAsync(null);

        viewModel.SelectedTransaction = viewModel.Transactions.Single();
        viewModel.BeginEditSelectedTransactionCommand.Execute(null);
        var expectedAmount = viewModel.DraftAmount;
        viewModel.UseSplitEditor = true;
        var split = viewModel.SplitDrafts.Single();
        split.EnvelopeId = envelopeId;
        split.Amount = expectedAmount;
        split.Category = "Food";
        split.Notes = "Split update";

        await viewModel.SubmitTransactionCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasError);
        Assert.Single(dataService.UpdateCalls);
        var update = dataService.UpdateCalls[0];
        Assert.True(update.ReplaceAllocation);
        Assert.Null(update.EnvelopeId);
        Assert.NotNull(update.Splits);
        Assert.Single(update.Splits!);
        Assert.Equal(envelopeId, update.Splits[0].EnvelopeId);
        Assert.Equal(expectedAmount, update.Splits[0].Amount);
    }

    [Fact]
    public async Task DeleteSelected_WhenEditing_CallsDeleteAndResetsEditorMode()
    {
        var accountId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var envelopeId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var transactionId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var dataService = new FakeTransactionsDataService(accountId, envelopeId, transactionId);
        var viewModel = new TransactionsViewModel(dataService);
        await viewModel.LoadAccountsCommand.ExecuteAsync(null);

        viewModel.SelectedTransaction = viewModel.Transactions.Single();
        viewModel.BeginEditSelectedTransactionCommand.Execute(null);
        Assert.True(viewModel.IsEditMode);

        await viewModel.DeleteSelectedTransactionCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasError);
        Assert.False(viewModel.IsEditMode);
        Assert.Equal("Transaction deleted.", viewModel.EditorStatusMessage);
        Assert.Single(dataService.DeleteCalls);
        Assert.Equal(transactionId, dataService.DeleteCalls[0]);
        Assert.Single(viewModel.DeletedTransactions);
        Assert.Equal(transactionId, viewModel.DeletedTransactions[0].Id);
    }

    [Fact]
    public async Task LoadAccounts_LoadsDeletedTransactionsUsingWindowDays()
    {
        var accountId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var envelopeId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var transactionId = Guid.Parse("30000000-0000-0000-0000-000000000099");
        var deletedTransaction = new TransactionListItemViewModel(
            transactionId,
            accountId,
            new DateTimeOffset(new DateTime(2026, 2, 20), TimeSpan.Zero),
            "Deleted Merchant",
            "Deleted Description",
            -7.25m,
            "Dining",
            envelopeId,
            "Groceries",
            [],
            new DateTimeOffset(new DateTime(2026, 3, 5), TimeSpan.Zero),
            "tester");
        var dataService = new FakeTransactionsDataService(accountId, envelopeId, Guid.NewGuid())
        {
            DeletedTransactions = [deletedTransaction]
        };
        var viewModel = new TransactionsViewModel(dataService)
        {
            DeletedWindowDays = "45"
        };

        await viewModel.LoadAccountsCommand.ExecuteAsync(null);

        Assert.Single(viewModel.DeletedTransactions);
        Assert.Equal(transactionId, viewModel.DeletedTransactions[0].Id);
        Assert.Equal(45, dataService.DeletedDaysRequests.Last());
    }

    [Fact]
    public async Task RestoreSelectedDeletedTransaction_RestoresAndReloadsLists()
    {
        var accountId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var envelopeId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var transactionId = Guid.Parse("30000000-0000-0000-0000-000000000077");
        var deletedTransaction = new TransactionListItemViewModel(
            transactionId,
            accountId,
            new DateTimeOffset(new DateTime(2026, 2, 15), TimeSpan.Zero),
            "Restore Merchant",
            "Restore Description",
            -11.10m,
            "Dining",
            envelopeId,
            "Groceries",
            [],
            new DateTimeOffset(new DateTime(2026, 3, 6), TimeSpan.Zero),
            "tester");
        var dataService = new FakeTransactionsDataService(accountId, envelopeId, Guid.NewGuid())
        {
            Transactions = [],
            DeletedTransactions = [deletedTransaction]
        };
        var viewModel = new TransactionsViewModel(dataService);
        await viewModel.LoadAccountsCommand.ExecuteAsync(null);
        viewModel.SelectedDeletedTransaction = viewModel.DeletedTransactions.Single();

        await viewModel.RestoreSelectedDeletedTransactionCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasError);
        Assert.Equal("Deleted transaction restored.", viewModel.EditorStatusMessage);
        Assert.Single(dataService.RestoreCalls);
        Assert.Equal(transactionId, dataService.RestoreCalls[0]);
        Assert.Contains(viewModel.Transactions, transaction => transaction.Id == transactionId);
        Assert.Empty(viewModel.DeletedTransactions);
        Assert.Equal(transactionId, viewModel.SelectedTransaction?.Id);
    }

    [Fact]
    public async Task DateRangeFilter_IncludesBoundaryDates()
    {
        var accountId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var envelopeId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var dataService = new FakeTransactionsDataService(accountId, envelopeId, Guid.NewGuid())
        {
            Transactions =
            [
                new TransactionListItemViewModel(
                    Guid.NewGuid(),
                    accountId,
                    new DateTimeOffset(new DateTime(2026, 3, 1), TimeSpan.Zero),
                    "M1",
                    "D1",
                    -10m,
                    "Dining",
                    envelopeId,
                    "Groceries",
                    []),
                new TransactionListItemViewModel(
                    Guid.NewGuid(),
                    accountId,
                    new DateTimeOffset(new DateTime(2026, 3, 5), TimeSpan.Zero),
                    "M2",
                    "D2",
                    -20m,
                    "Dining",
                    envelopeId,
                    "Groceries",
                    []),
                new TransactionListItemViewModel(
                    Guid.NewGuid(),
                    accountId,
                    new DateTimeOffset(new DateTime(2026, 3, 10), TimeSpan.Zero),
                    "M3",
                    "D3",
                    -30m,
                    "Dining",
                    envelopeId,
                    "Groceries",
                    [])
            ]
        };
        var viewModel = new TransactionsViewModel(dataService);
        await viewModel.LoadAccountsCommand.ExecuteAsync(null);

        viewModel.FromDateFilter = "2026-03-05";
        viewModel.ToDateFilter = "2026-03-10";

        Assert.Equal(string.Empty, viewModel.DateFilterErrorMessage);
        Assert.Equal(2, viewModel.Transactions.Count);
        Assert.Contains(viewModel.Transactions, tx => tx.OccurredDateDisplay == "2026-03-05");
        Assert.Contains(viewModel.Transactions, tx => tx.OccurredDateDisplay == "2026-03-10");
    }

    [Fact]
    public async Task DateRangeFilter_InvalidInput_ShowsValidationMessage()
    {
        var accountId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var envelopeId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var dataService = new FakeTransactionsDataService(accountId, envelopeId, Guid.NewGuid());
        var viewModel = new TransactionsViewModel(dataService);
        await viewModel.LoadAccountsCommand.ExecuteAsync(null);

        viewModel.FromDateFilter = "03/05/2026";

        Assert.Contains("yyyy-MM-dd", viewModel.DateFilterErrorMessage);
    }

    private sealed class FakeTransactionsDataService : ITransactionsDataService
    {
        private readonly Guid _accountId;
        private readonly Guid _envelopeId;
        private readonly Guid _transactionId;

        public FakeTransactionsDataService(Guid accountId, Guid envelopeId, Guid transactionId)
        {
            _accountId = accountId;
            _envelopeId = envelopeId;
            _transactionId = transactionId;
            Transactions =
            [
                new TransactionListItemViewModel(
                    _transactionId,
                    _accountId,
                    new DateTimeOffset(new DateTime(2026, 3, 1), TimeSpan.Zero),
                    "Original Merchant",
                    "Original Description",
                    -42.50m,
                    "Dining",
                    _envelopeId,
                    "Groceries",
                    [])
            ];
        }

        public List<UpdateCall> UpdateCalls { get; } = [];
        public List<Guid> DeleteCalls { get; } = [];
        public List<Guid> RestoreCalls { get; } = [];
        public List<int> DeletedDaysRequests { get; } = [];
        public IReadOnlyList<TransactionListItemViewModel> Transactions { get; set; }
        public IReadOnlyList<TransactionListItemViewModel> DeletedTransactions { get; set; } = [];

        public Task<IReadOnlyList<AccountListItemViewModel>> GetAccountsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AccountListItemViewModel>>(
            [
                new AccountListItemViewModel(_accountId, "Checking", "Bank", "$2,000.00")
            ]);
        }

        public Task<IReadOnlyList<EnvelopeOptionViewModel>> GetEnvelopesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<EnvelopeOptionViewModel>>(
            [
                new EnvelopeOptionViewModel(_envelopeId, "Groceries")
            ]);
        }

        public Task<IReadOnlyList<TransactionListItemViewModel>> GetTransactionsAsync(
            Guid accountId,
            CancellationToken cancellationToken = default)
        {
            Assert.Equal(_accountId, accountId);
            return Task.FromResult(Transactions);
        }

        public Task<IReadOnlyList<TransactionListItemViewModel>> GetDeletedTransactionsAsync(
            int days = 30,
            CancellationToken cancellationToken = default)
        {
            DeletedDaysRequests.Add(days);
            return Task.FromResult(DeletedTransactions);
        }

        public Task CreateTransactionAsync(
            Guid accountId,
            decimal amount,
            string description,
            string merchant,
            DateTimeOffset occurredAt,
            string? category,
            Guid? envelopeId,
            IReadOnlyList<TransactionSplitDraftViewModel>? splits,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task UpdateTransactionAsync(
            Guid transactionId,
            string description,
            string merchant,
            string? category,
            bool replaceAllocation,
            Guid? envelopeId,
            IReadOnlyList<TransactionSplitDraftViewModel>? splits,
            CancellationToken cancellationToken = default)
        {
            UpdateCalls.Add(new UpdateCall(
                transactionId,
                description,
                merchant,
                category,
                replaceAllocation,
                envelopeId,
                splits?.Select(static split => new SplitCall(
                        split.EnvelopeId,
                        split.Amount,
                        split.Category,
                        split.Notes))
                    .ToArray()));
            return Task.CompletedTask;
        }

        public Task DeleteTransactionAsync(
            Guid transactionId,
            CancellationToken cancellationToken = default)
        {
            DeleteCalls.Add(transactionId);
            var active = Transactions.FirstOrDefault(transaction => transaction.Id == transactionId);
            if (active is not null)
            {
                Transactions = Transactions.Where(transaction => transaction.Id != transactionId).ToArray();
                DeletedTransactions =
                [
                    ..DeletedTransactions,
                    new TransactionListItemViewModel(
                        active.Id,
                        active.AccountId,
                        active.OccurredAt,
                        active.Merchant,
                        active.Description,
                        active.Amount,
                        active.Category,
                        active.EnvelopeId,
                        active.Envelope,
                        active.Splits,
                        DateTimeOffset.UtcNow,
                        "test-user")
                ];
            }

            return Task.CompletedTask;
        }

        public Task RestoreTransactionAsync(
            Guid transactionId,
            CancellationToken cancellationToken = default)
        {
            RestoreCalls.Add(transactionId);
            var deleted = DeletedTransactions.FirstOrDefault(transaction => transaction.Id == transactionId);
            if (deleted is not null)
            {
                DeletedTransactions = DeletedTransactions.Where(transaction => transaction.Id != transactionId).ToArray();
                Transactions =
                [
                    ..Transactions,
                    new TransactionListItemViewModel(
                        deleted.Id,
                        deleted.AccountId,
                        deleted.OccurredAt,
                        deleted.Merchant,
                        deleted.Description,
                        deleted.Amount,
                        deleted.Category,
                        deleted.EnvelopeId,
                        deleted.Envelope,
                        deleted.Splits)
                ];
            }

            return Task.CompletedTask;
        }
    }

    private sealed record UpdateCall(
        Guid TransactionId,
        string Description,
        string Merchant,
        string? Category,
        bool ReplaceAllocation,
        Guid? EnvelopeId,
        IReadOnlyList<SplitCall>? Splits);

    private sealed record SplitCall(
        Guid? EnvelopeId,
        decimal Amount,
        string Category,
        string Notes);
}
