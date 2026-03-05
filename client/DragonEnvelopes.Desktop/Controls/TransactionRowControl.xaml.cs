using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DragonEnvelopes.Desktop.Controls;

public partial class TransactionRowControl : UserControl
{
    public static readonly DependencyProperty DateProperty = DependencyProperty.Register(
        nameof(Date),
        typeof(string),
        typeof(TransactionRowControl),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty MerchantProperty = DependencyProperty.Register(
        nameof(Merchant),
        typeof(string),
        typeof(TransactionRowControl),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty AmountProperty = DependencyProperty.Register(
        nameof(Amount),
        typeof(string),
        typeof(TransactionRowControl),
        new PropertyMetadata("$0.00"));

    public static readonly DependencyProperty EnvelopeProperty = DependencyProperty.Register(
        nameof(Envelope),
        typeof(string),
        typeof(TransactionRowControl),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty CategoryProperty = DependencyProperty.Register(
        nameof(Category),
        typeof(string),
        typeof(TransactionRowControl),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
        nameof(IsSelected),
        typeof(bool),
        typeof(TransactionRowControl),
        new PropertyMetadata(false));

    public static readonly DependencyProperty IsEditedProperty = DependencyProperty.Register(
        nameof(IsEdited),
        typeof(bool),
        typeof(TransactionRowControl),
        new PropertyMetadata(false));

    public static readonly DependencyProperty IsFlaggedProperty = DependencyProperty.Register(
        nameof(IsFlagged),
        typeof(bool),
        typeof(TransactionRowControl),
        new PropertyMetadata(false));

    public static readonly DependencyProperty EditCommandProperty = DependencyProperty.Register(
        nameof(EditCommand),
        typeof(ICommand),
        typeof(TransactionRowControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty SplitCommandProperty = DependencyProperty.Register(
        nameof(SplitCommand),
        typeof(ICommand),
        typeof(TransactionRowControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
        nameof(CommandParameter),
        typeof(object),
        typeof(TransactionRowControl),
        new PropertyMetadata(null));

    public TransactionRowControl()
    {
        InitializeComponent();
    }

    public string Date
    {
        get => (string)GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    public string Merchant
    {
        get => (string)GetValue(MerchantProperty);
        set => SetValue(MerchantProperty, value);
    }

    public string Amount
    {
        get => (string)GetValue(AmountProperty);
        set => SetValue(AmountProperty, value);
    }

    public string Envelope
    {
        get => (string)GetValue(EnvelopeProperty);
        set => SetValue(EnvelopeProperty, value);
    }

    public string Category
    {
        get => (string)GetValue(CategoryProperty);
        set => SetValue(CategoryProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool IsEdited
    {
        get => (bool)GetValue(IsEditedProperty);
        set => SetValue(IsEditedProperty, value);
    }

    public bool IsFlagged
    {
        get => (bool)GetValue(IsFlaggedProperty);
        set => SetValue(IsFlaggedProperty, value);
    }

    public ICommand EditCommand
    {
        get => (ICommand)GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    public ICommand SplitCommand
    {
        get => (ICommand)GetValue(SplitCommandProperty);
        set => SetValue(SplitCommandProperty, value);
    }

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }
}
