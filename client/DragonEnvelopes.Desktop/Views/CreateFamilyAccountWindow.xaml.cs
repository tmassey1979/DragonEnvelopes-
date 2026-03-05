using System.Net.Mail;
using System.Windows;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.Views;

public partial class CreateFamilyAccountWindow : Window
{
    private readonly IFamilyAccountService _familyAccountService;
    public string? CreatedEmail { get; private set; }

    public CreateFamilyAccountWindow(IFamilyAccountService familyAccountService)
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
        var validationError = ValidateInputs();
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            ValidationText.Text = validationError;
            ValidationText.Visibility = Visibility.Visible;
            return;
        }

        ValidationText.Visibility = Visibility.Collapsed;
        CreateButton.IsEnabled = false;

        var result = await _familyAccountService.CreateAsync(new CreateFamilyAccountRequest(
            FamilyNameBox.Text.Trim(),
            GuardianFirstNameBox.Text.Trim(),
            GuardianLastNameBox.Text.Trim(),
            EmailBox.Text.Trim(),
            PasswordBox.Password));

        CreateButton.IsEnabled = true;

        if (!result.Succeeded)
        {
            ValidationText.Text = result.Message;
            ValidationText.Visibility = Visibility.Visible;
            return;
        }

        MessageBox.Show(
            result.Message,
            "DragonEnvelopes",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        CreatedEmail = EmailBox.Text.Trim();
        DialogResult = true;
        Close();
    }

    private string? ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(FamilyNameBox.Text))
        {
            return "Family name is required.";
        }

        if (string.IsNullOrWhiteSpace(GuardianFirstNameBox.Text))
        {
            return "Primary guardian first name is required.";
        }

        if (string.IsNullOrWhiteSpace(GuardianLastNameBox.Text))
        {
            return "Primary guardian last name is required.";
        }

        if (string.IsNullOrWhiteSpace(EmailBox.Text))
        {
            return "Email is required.";
        }

        try
        {
            _ = new MailAddress(EmailBox.Text.Trim());
        }
        catch (FormatException)
        {
            return "Enter a valid email address.";
        }

        if (string.IsNullOrWhiteSpace(PasswordBox.Password))
        {
            return "Password is required.";
        }

        if (PasswordBox.Password.Length < 8)
        {
            return "Password must be at least 8 characters.";
        }

        if (!string.Equals(PasswordBox.Password, ConfirmPasswordBox.Password, StringComparison.Ordinal))
        {
            return "Password confirmation does not match.";
        }

        return null;
    }
}
