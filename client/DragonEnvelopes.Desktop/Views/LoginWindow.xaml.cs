using System.Windows;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    public LoginWindow(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
        InitializeComponent();
        LoginControl.SignInRequested += OnSignInRequested;
        LoginControl.CreateFamilyRequested += OnCreateFamilyRequested;
    }

    private async void OnSignInRequested(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(LoginControl.Username) || string.IsNullOrWhiteSpace(LoginControl.Password))
        {
            LoginControl.SetError("Enter both username/email and password.");
            return;
        }

        LoginControl.SetError(null);
        LoginControl.SetBusy(true);

        try
        {
            var result = await _mainWindowViewModel.SignInWithPasswordAsync(LoginControl.Username, LoginControl.Password);
            if (!result.Succeeded)
            {
                LoginControl.SetError(result.Message);
                return;
            }

            DialogResult = true;
            Close();
        }
        finally
        {
            LoginControl.SetBusy(false);
        }
    }

    private void OnCreateFamilyRequested(object? sender, EventArgs e)
    {
        MessageBox.Show(
            "Create Family Account window is coming next.",
            "DragonEnvelopes",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
