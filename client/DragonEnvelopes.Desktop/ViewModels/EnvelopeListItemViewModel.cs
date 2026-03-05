using CommunityToolkit.Mvvm.ComponentModel;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class EnvelopeListItemViewModel : ObservableObject
{
    public EnvelopeListItemViewModel(
        Guid id,
        string name,
        decimal monthlyBudget,
        decimal currentBalance,
        bool isArchived)
    {
        Id = id;
        Name = name;
        MonthlyBudget = monthlyBudget;
        CurrentBalance = currentBalance;
        IsArchived = isArchived;
    }

    public Guid Id { get; }

    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private decimal monthlyBudget;

    [ObservableProperty]
    private decimal currentBalance;

    [ObservableProperty]
    private bool isArchived;

    [ObservableProperty]
    private bool isSelected;

    public string MonthlyBudgetDisplay => MonthlyBudget.ToString("$#,##0.00");

    public string CurrentBalanceDisplay => CurrentBalance.ToString("$#,##0.00");
}
