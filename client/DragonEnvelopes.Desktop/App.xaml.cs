using System.Windows;
using DragonEnvelopes.Desktop.Services;
using DragonEnvelopes.Desktop.ViewModels;
using DragonEnvelopes.Desktop.Views;

namespace DragonEnvelopes.Desktop;

public partial class App : Application
{
    private void OnStartup(object sender, StartupEventArgs e)
    {
        var mainWindow = new MainWindow();
        if (mainWindow.DataContext is not MainWindowViewModel viewModel)
        {
            Shutdown(-1);
            return;
        }

        var loginWindow = new LoginWindow(
            viewModel,
            FamilyAccountServiceFactory.CreateDefault())
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        var loginSucceeded = loginWindow.ShowDialog() == true;
        if (!loginSucceeded)
        {
            Shutdown();
            return;
        }

        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
