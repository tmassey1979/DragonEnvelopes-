using CommunityToolkit.Mvvm.ComponentModel;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class OnboardingStepItemViewModel : ObservableObject
{
    public OnboardingStepItemViewModel(int index, string title)
    {
        Index = index;
        Title = title;
    }

    public int Index { get; }

    public string Title { get; }

    [ObservableProperty]
    private bool isCompleted;

    [ObservableProperty]
    private bool isCurrent;

    public string StatusLabel => IsCompleted
        ? "Completed"
        : IsCurrent
            ? "Current"
            : "Pending";

    partial void OnIsCompletedChanged(bool value) => OnPropertyChanged(nameof(StatusLabel));

    partial void OnIsCurrentChanged(bool value) => OnPropertyChanged(nameof(StatusLabel));
}
