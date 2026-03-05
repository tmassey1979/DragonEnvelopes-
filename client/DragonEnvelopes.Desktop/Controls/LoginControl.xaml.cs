using System.Windows;
using System.Windows.Controls;

namespace DragonEnvelopes.Desktop.Controls;

public partial class LoginControl : UserControl
{
    public event EventHandler? SignInRequested;

    public event EventHandler? CreateFamilyRequested;

    public event EventHandler? CancelRequested;

    public LoginControl()
    {
        InitializeComponent();
    }

    public string Username => UsernameBox.Text.Trim();

    public string Password => PasswordBox.Password;

    public void SetError(string? message)
    {
        var hasError = !string.IsNullOrWhiteSpace(message);
        ErrorTextBlock.Text = message ?? string.Empty;
        ErrorTextBlock.Visibility = hasError ? Visibility.Visible : Visibility.Collapsed;
    }

    public void SetBusy(bool busy)
    {
        UsernameBox.IsEnabled = !busy;
        PasswordBox.IsEnabled = !busy;
        SignInButton.IsEnabled = !busy;
        CancelButton.IsEnabled = !busy;
        CreateFamilyButton.IsEnabled = !busy;
        SignInButton.Content = busy ? "Signing In..." : "Sign In";
    }

    private void OnSignInClicked(object sender, RoutedEventArgs e)
    {
        SignInRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnCreateFamilyClicked(object sender, RoutedEventArgs e)
    {
        CreateFamilyRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
