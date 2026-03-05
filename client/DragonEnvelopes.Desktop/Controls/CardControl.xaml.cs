using System.Windows;
using System.Windows.Controls;

namespace DragonEnvelopes.Desktop.Controls;

public partial class CardControl : UserControl
{
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header),
        typeof(string),
        typeof(CardControl),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubheaderProperty = DependencyProperty.Register(
        nameof(Subheader),
        typeof(string),
        typeof(CardControl),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(
        nameof(IsLoading),
        typeof(bool),
        typeof(CardControl),
        new PropertyMetadata(false));

    public static readonly DependencyProperty IsEmptyProperty = DependencyProperty.Register(
        nameof(IsEmpty),
        typeof(bool),
        typeof(CardControl),
        new PropertyMetadata(false));

    public static readonly DependencyProperty EmptyMessageProperty = DependencyProperty.Register(
        nameof(EmptyMessage),
        typeof(string),
        typeof(CardControl),
        new PropertyMetadata("No content available."));

    public CardControl()
    {
        InitializeComponent();
    }

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string Subheader
    {
        get => (string)GetValue(SubheaderProperty);
        set => SetValue(SubheaderProperty, value);
    }

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public bool IsEmpty
    {
        get => (bool)GetValue(IsEmptyProperty);
        set => SetValue(IsEmptyProperty, value);
    }

    public string EmptyMessage
    {
        get => (string)GetValue(EmptyMessageProperty);
        set => SetValue(EmptyMessageProperty, value);
    }
}
