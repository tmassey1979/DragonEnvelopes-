namespace DragonEnvelopes.Desktop.ViewModels;

public sealed class EnvelopeTileViewModel
{
    public EnvelopeTileViewModel(string name, string monthlyBudget, string currentBalance, bool isSelected = false)
    {
        Name = name;
        MonthlyBudget = monthlyBudget;
        CurrentBalance = currentBalance;
        IsSelected = isSelected;
    }

    public string Name { get; }

    public string MonthlyBudget { get; }

    public string CurrentBalance { get; }

    public bool IsSelected { get; }
}
