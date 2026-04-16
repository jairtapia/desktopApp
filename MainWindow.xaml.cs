using Microsoft.UI.Xaml;
using DesktopAssistant.Helpers;
using DesktopAssistant.Services;
using DesktopAssistant.Views;

namespace DesktopAssistant;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.SetIcon("Assets/AppIcon.ico");

        // Set the root frame in the navigation service
        var navigationService = App.GetService<NavigationService>();
        navigationService.RootFrame = RootFrame;

        // Determine initial page based on auth state
        var authService = App.GetService<IAuthService>();

        if (authService.IsAuthenticated)
        {
            if (!SecureStorage.IsTutorialCompleted())
            {
                RootFrame.Navigate(typeof(TutorialPage));
            }
            else
            {
                RootFrame.Navigate(typeof(ShellPage));
            }
        }
        else
        {
            RootFrame.Navigate(typeof(LoginPage));
        }

        // Set window size
        var appWindow = this.AppWindow;
        appWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));
    }
}
