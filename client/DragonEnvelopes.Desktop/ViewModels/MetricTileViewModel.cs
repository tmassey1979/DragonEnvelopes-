namespace DragonEnvelopes.Desktop.ViewModels;

public sealed class MetricTileViewModel
{
    public MetricTileViewModel(
        string label,
        string value,
        string trend,
        MetricTrendDirection trendDirection,
        bool isLoading = false,
        bool isEmpty = false,
        string emptyMessage = "No metric data")
    {
        Label = label;
        Value = value;
        Trend = trend;
        TrendDirection = trendDirection;
        IsLoading = isLoading;
        IsEmpty = isEmpty;
        EmptyMessage = emptyMessage;
    }

    public string Label { get; }

    public string Value { get; }

    public string Trend { get; }

    public MetricTrendDirection TrendDirection { get; }

    public bool IsLoading { get; }

    public bool IsEmpty { get; }

    public string EmptyMessage { get; }
}
