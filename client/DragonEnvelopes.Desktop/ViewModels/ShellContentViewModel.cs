namespace DragonEnvelopes.Desktop.ViewModels;

public sealed class ShellContentViewModel
{
    public ShellContentViewModel(
        string title,
        string description,
        string emptyStateTitle,
        string emptyStateBody,
        IReadOnlyList<MetricTileViewModel>? metrics = null,
        bool isLoading = false,
        bool isEmpty = false)
    {
        Title = title;
        Description = description;
        EmptyStateTitle = emptyStateTitle;
        EmptyStateBody = emptyStateBody;
        Metrics = metrics ?? [];
        IsLoading = isLoading;
        IsEmpty = isEmpty;
    }

    public string Title { get; }

    public string Description { get; }

    public string EmptyStateTitle { get; }

    public string EmptyStateBody { get; }

    public IReadOnlyList<MetricTileViewModel> Metrics { get; }

    public bool IsLoading { get; }

    public bool IsEmpty { get; }
}
