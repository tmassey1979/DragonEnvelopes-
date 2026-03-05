using CommunityToolkit.Mvvm.ComponentModel;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class NavigationItemViewModel : ObservableObject
{
    public NavigationItemViewModel(string key, string label, string glyph, object content)
    {
        Key = key;
        Label = label;
        Glyph = glyph;
        Content = content;
    }

    public string Key { get; }

    public string Label { get; }

    public string Glyph { get; }

    public object Content { get; }

    [ObservableProperty]
    private bool isSelected;
}
