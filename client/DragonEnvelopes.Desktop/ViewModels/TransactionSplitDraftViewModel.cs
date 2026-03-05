using CommunityToolkit.Mvvm.ComponentModel;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class TransactionSplitDraftViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid? envelopeId;

    [ObservableProperty]
    private decimal amount;

    [ObservableProperty]
    private string category = string.Empty;

    [ObservableProperty]
    private string notes = string.Empty;
}
