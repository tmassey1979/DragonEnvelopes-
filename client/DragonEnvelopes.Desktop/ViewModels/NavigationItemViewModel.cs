using CommunityToolkit.Mvvm.ComponentModel;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class NavigationItemViewModel : ObservableObject
{
    public NavigationItemViewModel(string key, string label, string glyph, object content, string? requiredRole)
    {
        Key = key;
        Label = label;
        Glyph = glyph;
        Content = content;
        RequiredRole = requiredRole;
    }

    public string Key { get; }

    public string Label { get; }

    public string Glyph { get; }

    public object Content { get; }

    public string? RequiredRole { get; }

    [ObservableProperty]
    private bool isEnabled = true;

    [ObservableProperty]
    private bool isSelected;
}
