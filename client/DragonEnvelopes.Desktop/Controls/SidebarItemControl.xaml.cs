using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DragonEnvelopes.Desktop.Controls;

public partial class SidebarItemControl : UserControl
{
    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph),
        typeof(string),
        typeof(SidebarItemControl),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label),
        typeof(string),
        typeof(SidebarItemControl),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
        nameof(IsSelected),
        typeof(bool),
        typeof(SidebarItemControl),
        new PropertyMetadata(false));

    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command),
        typeof(ICommand),
        typeof(SidebarItemControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
        nameof(CommandParameter),
        typeof(object),
        typeof(SidebarItemControl),
        new PropertyMetadata(null));

    public SidebarItemControl()
    {
        InitializeComponent();
    }

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }
}
