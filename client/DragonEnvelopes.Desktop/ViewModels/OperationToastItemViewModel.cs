using CommunityToolkit.Mvvm.ComponentModel;
using DragonEnvelopes.Desktop.Services;
using System.Windows.Media;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class OperationToastItemViewModel : ObservableObject
{
    public OperationToastItemViewModel(OperationToastLevel level, string message, bool isTransient)
    {
        Id = Guid.NewGuid();
        Level = level;
        Message = message;
        IsTransient = isTransient;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }
    public OperationToastLevel Level { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public bool IsTransient { get; }

    [ObservableProperty]
    private string message;

    public string LevelLabel => Level switch
    {
        OperationToastLevel.Success => "Success",
        OperationToastLevel.Error => "Error",
        _ => "Info"
    };

    public Brush LevelBrush => Level switch
    {
        OperationToastLevel.Success => new SolidColorBrush(Color.FromRgb(0x14, 0x8A, 0x3B)),
        OperationToastLevel.Error => new SolidColorBrush(Color.FromRgb(0x9F, 0x12, 0x39)),
        _ => new SolidColorBrush(Color.FromRgb(0x26, 0x4A, 0x73))
    };
}
