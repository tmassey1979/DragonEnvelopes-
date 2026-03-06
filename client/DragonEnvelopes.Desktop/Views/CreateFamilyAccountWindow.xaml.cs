using System.Windows;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.Views;

public partial class CreateFamilyAccountWindow : Window
{
    private readonly IFamilyAccountService _familyAccountService;
    private bool _isAccountStep = true;

    public string? CreatedEmail { get; private set; }

    public CreateFamilyAccountWindow(IFamilyAccountService familyAccountService)
    {
        _familyAccountService = familyAccountService;
        InitializeComponent();
        ShowAccountStep();
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnBackClicked(object sender, RoutedEventArgs e)
    {
        ShowAccountStep();
    }

    private void OnNextClicked(object sender, RoutedEventArgs e)
    {
        var validationError = OnboardingRegistrationValidator.ValidateAccountStep(
            GuardianFirstNameBox.Text,
            GuardianLastNameBox.Text,
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
        ShowFamilyStep();
    }

    private async void OnCreateClicked(object sender, RoutedEventArgs e)
    {
        var validationError = ValidateFinalStep();
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

    private string? ValidateFinalStep()
    {
        if (_isAccountStep)
        {
            return "Complete account registration before creating the family.";
        }

        var accountValidationError = OnboardingRegistrationValidator.ValidateAccountStep(
            GuardianFirstNameBox.Text,
            GuardianLastNameBox.Text,
            EmailBox.Text,
            PasswordBox.Password,
            ConfirmPasswordBox.Password);
        if (!string.IsNullOrWhiteSpace(accountValidationError))
        {
            return accountValidationError;
        }

        return OnboardingRegistrationValidator.ValidateFamilyStep(FamilyNameBox.Text);
    }

    private void ShowAccountStep()
    {
        _isAccountStep = true;
        AccountStepPanel.Visibility = Visibility.Visible;
        FamilyStepPanel.Visibility = Visibility.Collapsed;
        BackButton.Visibility = Visibility.Collapsed;
        NextButton.Visibility = Visibility.Visible;
        CreateButton.Visibility = Visibility.Collapsed;
        StepDescriptionText.Text = "Step 1 of 2: create your account credentials.";
    }

    private void ShowFamilyStep()
    {
        _isAccountStep = false;
        AccountStepPanel.Visibility = Visibility.Collapsed;
        FamilyStepPanel.Visibility = Visibility.Visible;
        BackButton.Visibility = Visibility.Visible;
        NextButton.Visibility = Visibility.Collapsed;
        CreateButton.Visibility = Visibility.Visible;
        StepDescriptionText.Text = "Step 2 of 2: configure your family workspace.";
    }
}
