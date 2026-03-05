using System.Windows;
using System.Windows.Controls;

namespace DragonEnvelopes.Desktop.Controls;

public partial class EnvelopeTileControl : UserControl
{
    public static readonly DependencyProperty EnvelopeNameProperty = DependencyProperty.Register(
        nameof(EnvelopeName),
        typeof(string),
        typeof(EnvelopeTileControl),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty MonthlyBudgetProperty = DependencyProperty.Register(
        nameof(MonthlyBudget),
        typeof(string),
        typeof(EnvelopeTileControl),
        new PropertyMetadata("$0"));

    public static readonly DependencyProperty CurrentBalanceProperty = DependencyProperty.Register(
        nameof(CurrentBalance),
        typeof(string),
        typeof(EnvelopeTileControl),
        new PropertyMetadata("$0"));

    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
        nameof(IsSelected),
        typeof(bool),
        typeof(EnvelopeTileControl),
        new PropertyMetadata(false));

    public EnvelopeTileControl()
    {
        InitializeComponent();
    }

    public string EnvelopeName
    {
        get => (string)GetValue(EnvelopeNameProperty);
        set => SetValue(EnvelopeNameProperty, value);
    }

    public string MonthlyBudget
    {
        get => (string)GetValue(MonthlyBudgetProperty);
        set => SetValue(MonthlyBudgetProperty, value);
    }

    public string CurrentBalance
    {
        get => (string)GetValue(CurrentBalanceProperty);
        set => SetValue(CurrentBalanceProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }
}
