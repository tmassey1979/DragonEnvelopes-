using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class OnboardingEnvelopeSuggestionViewModel : ObservableObject
{
    public OnboardingEnvelopeSuggestionViewModel(string category, string envelopeName, string monthlyBudget)
    {
        Category = category;
        EnvelopeName = envelopeName;
        MonthlyBudget = monthlyBudget;
    }

    [ObservableProperty]
    private string category = string.Empty;

    [ObservableProperty]
    private string envelopeName = string.Empty;

    [ObservableProperty]
    private string monthlyBudget = "0";

    public decimal GetMonthlyBudgetOrZero()
    {
        if (decimal.TryParse(
                MonthlyBudget,
                NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                CultureInfo.CurrentCulture,
                out var current))
        {
            return Math.Max(0m, current);
        }

        if (decimal.TryParse(
                MonthlyBudget,
                NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                CultureInfo.InvariantCulture,
                out var invariant))
        {
            return Math.Max(0m, invariant);
        }

        return 0m;
    }
}
