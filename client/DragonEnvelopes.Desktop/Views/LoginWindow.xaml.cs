using System.Windows;
using DragonEnvelopes.Desktop.Auth;
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
        LoginControl.RedeemInviteRequested += OnRedeemInviteRequested;
        LoginControl.CreateInviteAccountRequested += OnCreateInviteAccountRequested;
        LoginControl.GetStartedRequested += OnGetStartedRequested;
        LoginControl.CancelRequested += OnCancelRequested;
    }

    private async void OnSignInRequested(object? sender, EventArgs e)
    {
        var result = await SignInFromCredentialsAsync();
        if (!result.Succeeded)
        {
            return;
        }

        DialogResult = true;
        Close();
    }

    private async void OnRedeemInviteRequested(object? sender, EventArgs e)
    {
        var signInResult = await SignInFromCredentialsAsync();
        if (!signInResult.Succeeded)
        {
            return;
        }

        var redeemWindow = new RedeemFamilyInviteWindow(_familyAccountService, LoginControl.Username)
        {
            Owner = this
        };

        var redeemed = redeemWindow.ShowDialog() == true;
        if (!redeemed)
        {
            LoginControl.SetStatus("Invite redemption canceled.");
            return;
        }

        await _mainWindowViewModel.RefreshFamilyContextForCurrentSessionAsync();
        if (_mainWindowViewModel.AvailableFamilies.Count == 0)
        {
            LoginControl.SetError("Invite redeemed, but no family context was loaded. Try signing in again.");
            return;
        }

        LoginControl.SetStatus("Invite redeemed successfully.");
        DialogResult = true;
        Close();
    }

    private async void OnCreateInviteAccountRequested(object? sender, EventArgs e)
    {
        var createInviteAccountWindow = new CreateInviteAccountWindow(_familyAccountService)
        {
            Owner = this
        };

        var created = createInviteAccountWindow.ShowDialog() == true;
        if (!created
            || string.IsNullOrWhiteSpace(createInviteAccountWindow.CreatedEmail)
            || string.IsNullOrWhiteSpace(createInviteAccountWindow.CreatedPassword))
        {
            return;
        }

        LoginControl.SetUsername(createInviteAccountWindow.CreatedEmail);
        var result = await SignInFromCredentialsAsync(
            createInviteAccountWindow.CreatedEmail,
            createInviteAccountWindow.CreatedPassword);
        if (!result.Succeeded)
        {
            LoginControl.ShowSignInView();
            LoginControl.SetError(result.Message);
            return;
        }

        await _mainWindowViewModel.RefreshFamilyContextForCurrentSessionAsync();
        if (_mainWindowViewModel.AvailableFamilies.Count == 0)
        {
            LoginControl.ShowSignInView();
            LoginControl.SetError("Invite account created but no family context loaded. Try signing in again.");
            return;
        }

        DialogResult = true;
        Close();
    }

    private void OnGetStartedRequested(object? sender, EventArgs e)
    {
        var createFamilyWindow = new CreateFamilyAccountWindow(_familyAccountService)
        {
            Owner = this
        };

        var created = createFamilyWindow.ShowDialog() == true;
        if (!created)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(createFamilyWindow.CreatedEmail))
        {
            LoginControl.SetUsername(createFamilyWindow.CreatedEmail);
        }

        LoginControl.ShowSignInView();
        LoginControl.SetStatus("Family account created. Sign in with your new credentials.");
    }

    private void OnCancelRequested(object? sender, EventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async Task<AuthSignInResult> SignInFromCredentialsAsync()
    {
        return await SignInFromCredentialsAsync(LoginControl.Username, LoginControl.Password);
    }

    private async Task<AuthSignInResult> SignInFromCredentialsAsync(string usernameOrEmail, string password)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
        {
            LoginControl.SetError("Enter both username/email and password.");
            return new AuthSignInResult(false, false, "Username/email and password are required.");
        }

        LoginControl.SetError(null);
        LoginControl.SetBusy(true);

        try
        {
            var result = await _mainWindowViewModel.SignInWithPasswordAsync(usernameOrEmail, password);
            if (!result.Succeeded)
            {
                LoginControl.SetError(result.Message);
            }

            return result;
        }
        finally
        {
            LoginControl.SetBusy(false);
        }
    }
}
