using CommunityToolkit.Mvvm.ComponentModel;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class OnboardingStripeProvisioningItemViewModel : ObservableObject
{
    public OnboardingStripeProvisioningItemViewModel(
        Guid envelopeId,
        string envelopeName,
        string displayName,
        bool isSelected,
        string status,
        string statusDetail)
    {
        EnvelopeId = envelopeId;
        EnvelopeName = envelopeName;
        DisplayName = displayName;
        IsSelected = isSelected;
        Status = status;
        StatusDetail = statusDetail;
    }

    public Guid EnvelopeId { get; }

    public string EnvelopeName { get; }

    [ObservableProperty]
    private string displayName = string.Empty;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private string status = "NotProvisioned";

    [ObservableProperty]
    private string statusDetail = string.Empty;

    public bool IsProvisioned => Status.Equals("Provisioned", StringComparison.OrdinalIgnoreCase);
    public bool IsFailed => Status.Equals("Failed", StringComparison.OrdinalIgnoreCase);

    partial void OnStatusChanged(string value)
    {
        OnPropertyChanged(nameof(IsProvisioned));
        OnPropertyChanged(nameof(IsFailed));
    }
}
