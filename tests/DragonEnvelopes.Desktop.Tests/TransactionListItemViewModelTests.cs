using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Tests;

public sealed class TransactionListItemViewModelTests
{
    [Fact]
    public void AllocationDisplay_SingleEnvelope_ReturnsEnvelopeName()
    {
        var viewModel = CreateItem(envelope: "Groceries", splits: []);

        Assert.Equal("Groceries", viewModel.AllocationDisplay);
    }

    [Fact]
    public void AllocationDisplay_WithSplits_ReturnsSplitCount()
    {
        var viewModel = CreateItem(
            envelope: "-",
            splits:
            [
                new TransactionSplitSnapshotViewModel(Guid.NewGuid(), -10m, "Dining", null),
                new TransactionSplitSnapshotViewModel(Guid.NewGuid(), -12.5m, "Household", null)
            ]);

        Assert.Equal("Split (2)", viewModel.AllocationDisplay);
    }

    [Fact]
    public void AllocationDisplay_UnassignedWithoutSplits_ReturnsUnassigned()
    {
        var viewModel = CreateItem(envelope: "-", splits: []);

        Assert.Equal("Unassigned", viewModel.AllocationDisplay);
    }

    [Fact]
    public void AllocationDisplay_TransferDebit_ReturnsTransferOutSummary()
    {
        var transferId = Guid.NewGuid();
        var viewModel = new TransactionListItemViewModel(
            id: Guid.NewGuid(),
            accountId: Guid.NewGuid(),
            occurredAt: DateTimeOffset.UtcNow,
            merchant: "Envelope Transfer",
            description: "Envelope transfer",
            amount: -25m,
            category: "Envelope Transfer",
            envelopeId: Guid.NewGuid(),
            envelope: "Groceries",
            splits: [],
            transferId: transferId,
            transferCounterpartyEnvelopeId: Guid.NewGuid(),
            transferDirection: "Debit",
            transferCounterpartyEnvelopeName: "Fuel");

        Assert.Equal("Out -> Fuel", viewModel.AllocationDisplay);
        Assert.True(viewModel.IsTransfer);
    }

    [Fact]
    public void StatusBadgeText_UsesApprovalStatusWhenPresent()
    {
        var viewModel = CreateItem(envelope: "Groceries", splits: []);
        viewModel.SetApprovalStatus("Approved");

        Assert.Equal("Approved", viewModel.StatusBadgeText);
    }

    private static TransactionListItemViewModel CreateItem(
        string envelope,
        IReadOnlyList<TransactionSplitSnapshotViewModel> splits)
    {
        return new TransactionListItemViewModel(
            id: Guid.NewGuid(),
            accountId: Guid.NewGuid(),
            occurredAt: DateTimeOffset.UtcNow,
            merchant: "Merchant",
            description: "Description",
            amount: -42m,
            category: "Dining",
            envelopeId: null,
            envelope: envelope,
            splits: splits);
    }
}
