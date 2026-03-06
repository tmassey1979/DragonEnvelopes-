using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DragonEnvelopes.Desktop.Auth;

namespace DragonEnvelopes.Desktop.Controls;

public partial class LoginControl : UserControl
{
    private readonly LoginEntryState _entryState = new();

    public event EventHandler? SignInRequested;

    public event EventHandler? GetStartedRequested;

    public event EventHandler? CancelRequested;

    public LoginControl()
    {
        InitializeComponent();
        ApplyEntryState();
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

    public void ShowSignInView()
    {
        _entryState.ShowSignInView();
        ApplyEntryState();
        UsernameBox.Focus();
    }

    public void ShowWelcomeView()
    {
        _entryState.ShowWelcomeView();
        ApplyEntryState();
    }

    public void SetBusy(bool busy)
    {
        CancelWelcomeButton.IsEnabled = !busy;
        GetStartedButton.IsEnabled = !busy;
        RouteToSignInButton.IsEnabled = !busy;
        BackButton.IsEnabled = !busy;
        UsernameBox.IsEnabled = !busy;
        PasswordBox.IsEnabled = !busy;
        SignInButton.IsEnabled = !busy;
        CancelButton.IsEnabled = !busy;
        SignInButton.Content = busy ? "Signing In..." : "Sign In";
    }

    private void OnSignInClicked(object sender, RoutedEventArgs e)
    {
        SignInRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnGetStartedClicked(object sender, RoutedEventArgs e)
    {
        _entryState.HandleWelcomeAction(LoginWelcomeAction.GetStarted);
        GetStartedRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnRouteToSignInClicked(object sender, RoutedEventArgs e)
    {
        _entryState.HandleWelcomeAction(LoginWelcomeAction.SignIn);
        ApplyEntryState();
        UsernameBox.Focus();
    }

    private void OnBackClicked(object sender, RoutedEventArgs e)
    {
        ShowWelcomeView();
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyEntryState()
    {
        WelcomePanel.Visibility = _entryState.IsSignInViewVisible ? Visibility.Collapsed : Visibility.Visible;
        SignInPanel.Visibility = _entryState.IsSignInViewVisible ? Visibility.Visible : Visibility.Collapsed;
        if (!_entryState.IsSignInViewVisible)
        {
            SetStatus(null);
        }
    }
}
