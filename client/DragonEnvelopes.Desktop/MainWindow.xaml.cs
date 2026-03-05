using System.Windows;
using DragonEnvelopes.Desktop.Services;
using DragonEnvelopes.Desktop.ViewModels;
using DragonEnvelopes.Desktop.Views;

namespace DragonEnvelopes.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnAuthButtonClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.IsAuthenticated)
        {
            await viewModel.SignOutAsync();
            return;
        }

        var loginWindow = new LoginWindow(
            viewModel,
            FamilyAccountServiceFactory.CreateDefault())
        {
            Owner = this
        };

        loginWindow.ShowDialog();
    }
}
