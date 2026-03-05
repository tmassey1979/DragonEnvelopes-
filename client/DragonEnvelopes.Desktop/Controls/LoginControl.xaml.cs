using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        SetStatus(message, isError: true);
    }

    public void SetStatus(string? message, bool isError = false)
    {
        var hasMessage = !string.IsNullOrWhiteSpace(message);
        StatusTextBlock.Text = message ?? string.Empty;
        StatusTextBlock.Visibility = hasMessage ? Visibility.Visible : Visibility.Collapsed;
        StatusTextBlock.Foreground = isError
            ? (Brush)FindResource("AccentBrush")
            : (Brush)FindResource("SecondaryTextBrush");
    }

    public void SetUsername(string usernameOrEmail)
    {
        UsernameBox.Text = usernameOrEmail ?? string.Empty;
        UsernameBox.CaretIndex = UsernameBox.Text.Length;
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
