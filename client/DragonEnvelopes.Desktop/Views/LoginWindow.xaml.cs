using System.Windows;
using DragonEnvelopes.Desktop.Services;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IFamilyAccountService _familyAccountService;

    public LoginWindow(
        MainWindowViewModel mainWindowViewModel,
        IFamilyAccountService familyAccountService)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _familyAccountService = familyAccountService;
        InitializeComponent();
        LoginControl.SignInRequested += OnSignInRequested;
        LoginControl.CreateFamilyRequested += OnCreateFamilyRequested;
        LoginControl.CancelRequested += OnCancelRequested;
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
        var createFamilyWindow = new CreateFamilyAccountWindow(_familyAccountService)
        {
            Owner = this
        };

        createFamilyWindow.ShowDialog();
    }

    private void OnCancelRequested(object? sender, EventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
