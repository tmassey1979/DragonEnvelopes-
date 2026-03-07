using System.Windows;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.Views;

public partial class RedeemFamilyInviteWindow : Window
{
    private readonly IFamilyAccountService _familyAccountService;
    private readonly string? _usernameOrEmailHint;

    public Guid? RedeemedFamilyId { get; private set; }

    public RedeemFamilyInviteWindow(
        IFamilyAccountService familyAccountService,
        string? usernameOrEmailHint)
    {
        _familyAccountService = familyAccountService;
        _usernameOrEmailHint = string.IsNullOrWhiteSpace(usernameOrEmailHint)
            ? null
            : usernameOrEmailHint.Trim();
        InitializeComponent();
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void OnRedeemClicked(object sender, RoutedEventArgs e)
    {
        var inviteToken = InviteTokenBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(inviteToken))
        {
            ValidationText.Text = "Invite token is required.";
            ValidationText.Visibility = Visibility.Visible;
            return;
        }

        ValidationText.Visibility = Visibility.Collapsed;
        SetBusy(true);

        var memberNameHint = _usernameOrEmailHint;
        var memberEmailHint = _usernameOrEmailHint?.Contains('@') == true
            ? _usernameOrEmailHint
            : null;

        var result = await _familyAccountService.RedeemInviteAsync(
            inviteToken,
            memberNameHint,
            memberEmailHint);

        SetBusy(false);

        if (!result.Succeeded)
        {
            ValidationText.Text = result.Message;
            ValidationText.Visibility = Visibility.Visible;
            return;
        }

        RedeemedFamilyId = result.FamilyId;
        DialogResult = true;
        Close();
    }

    private void SetBusy(bool busy)
    {
        InviteTokenBox.IsEnabled = !busy;
        CancelButton.IsEnabled = !busy;
        RedeemButton.IsEnabled = !busy;
        RedeemButton.Content = busy ? "Redeeming..." : "Redeem";
    }
}
