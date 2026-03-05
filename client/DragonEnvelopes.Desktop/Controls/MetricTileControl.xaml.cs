using System.Windows;
using System.Windows.Controls;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Controls;

public partial class MetricTileControl : UserControl
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label),
        typeof(string),
        typeof(MetricTileControl),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(string),
        typeof(MetricTileControl),
        new PropertyMetadata("--"));

    public static readonly DependencyProperty TrendProperty = DependencyProperty.Register(
        nameof(Trend),
        typeof(string),
        typeof(MetricTileControl),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TrendDirectionProperty = DependencyProperty.Register(
        nameof(TrendDirection),
        typeof(MetricTrendDirection),
        typeof(MetricTileControl),
        new PropertyMetadata(MetricTrendDirection.Neutral));

    public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(
        nameof(IsLoading),
        typeof(bool),
        typeof(MetricTileControl),
        new PropertyMetadata(false));

    public static readonly DependencyProperty IsEmptyProperty = DependencyProperty.Register(
        nameof(IsEmpty),
        typeof(bool),
        typeof(MetricTileControl),
        new PropertyMetadata(false));

    public static readonly DependencyProperty EmptyMessageProperty = DependencyProperty.Register(
        nameof(EmptyMessage),
        typeof(string),
        typeof(MetricTileControl),
        new PropertyMetadata("No metric data"));

    public MetricTileControl()
    {
        InitializeComponent();
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Trend
    {
        get => (string)GetValue(TrendProperty);
        set => SetValue(TrendProperty, value);
    }

    public MetricTrendDirection TrendDirection
    {
        get => (MetricTrendDirection)GetValue(TrendDirectionProperty);
        set => SetValue(TrendDirectionProperty, value);
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
