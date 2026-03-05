namespace DragonEnvelopes.Desktop.ViewModels;

public sealed class ShellContentViewModel
{
    public ShellContentViewModel(string title, string description, string emptyStateTitle, string emptyStateBody)
    {
        Title = title;
        Description = description;
        EmptyStateTitle = emptyStateTitle;
        EmptyStateBody = emptyStateBody;
    }

    public string Title { get; }

    public string Description { get; }

    public string EmptyStateTitle { get; }

    public string EmptyStateBody { get; }
}
