using CommunityToolkit.Mvvm.ComponentModel;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class BudgetAllocationEnvelopeViewModel : ObservableObject
{
    public BudgetAllocationEnvelopeViewModel(
        Guid envelopeId,
        string name,
        string monthlyBudget,
        string currentBalance,
        string allocationPercent,
        string balancePercent,
        bool isArchived,
        string rolloverMode,
        decimal? rolloverCap)
    {
        EnvelopeId = envelopeId;
        Name = name;
        MonthlyBudget = monthlyBudget;
        CurrentBalance = currentBalance;
        AllocationPercent = allocationPercent;
        BalancePercent = balancePercent;
        IsArchived = isArchived;
        DraftRolloverMode = rolloverMode;
        DraftRolloverCap = rolloverCap;
    }

    public Guid EnvelopeId { get; }
    public string Name { get; }
    public string MonthlyBudget { get; }
    public string CurrentBalance { get; }
    public string AllocationPercent { get; }
    public string BalancePercent { get; }
    public bool IsArchived { get; }

    [ObservableProperty]
    private string draftRolloverMode;

    [ObservableProperty]
    private decimal? draftRolloverCap;

    public string RolloverCapDisplay => DraftRolloverCap.HasValue ? DraftRolloverCap.Value.ToString("0.00") : "--";
}
