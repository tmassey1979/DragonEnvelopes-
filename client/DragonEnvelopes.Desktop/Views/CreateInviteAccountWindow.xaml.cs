using System.Windows;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.Views;

public partial class CreateInviteAccountWindow : Window
{
    private readonly IFamilyAccountService _familyAccountService;

    public string? CreatedEmail { get; private set; }
    public string? CreatedPassword { get; private set; }

    public CreateInviteAccountWindow(IFamilyAccountService familyAccountService)
    {
        _familyAccountService = familyAccountService;
        InitializeComponent();
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void OnCreateClicked(object sender, RoutedEventArgs e)
    {
        var validationError = OnboardingRegistrationValidator.ValidateInviteRegistrationStep(
            InviteTokenBox.Text,
            FirstNameBox.Text,
            LastNameBox.Text,
            EmailBox.Text,
            PasswordBox.Password,
            ConfirmPasswordBox.Password);

        if (!string.IsNullOrWhiteSpace(validationError))
        {
            ValidationText.Text = validationError;
            ValidationText.Visibility = Visibility.Visible;
            return;
        }

        ValidationText.Visibility = Visibility.Collapsed;
        SetBusy(true);

        var result = await _familyAccountService.RegisterFromInviteAsync(
            new RegisterFamilyInviteAccountRequestData(
                InviteTokenBox.Text.Trim(),
                FirstNameBox.Text.Trim(),
                LastNameBox.Text.Trim(),
                EmailBox.Text.Trim(),
                PasswordBox.Password));

        SetBusy(false);

        if (!result.Succeeded)
        {
            ValidationText.Text = result.Message;
            ValidationText.Visibility = Visibility.Visible;
            return;
        }

        CreatedEmail = EmailBox.Text.Trim();
        CreatedPassword = PasswordBox.Password;
        DialogResult = true;
        Close();
    }

    private void SetBusy(bool busy)
    {
        InviteTokenBox.IsEnabled = !busy;
        FirstNameBox.IsEnabled = !busy;
        LastNameBox.IsEnabled = !busy;
        EmailBox.IsEnabled = !busy;
        PasswordBox.IsEnabled = !busy;
        ConfirmPasswordBox.IsEnabled = !busy;
        CancelButton.IsEnabled = !busy;
        CreateButton.IsEnabled = !busy;
        CreateButton.Content = busy ? "Creating..." : "Create Account";
    }
}
