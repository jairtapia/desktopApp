using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DesktopAssistant.Services;

namespace DesktopAssistant.Views;

public sealed partial class ShellPage : Page
{
    private readonly NavigationService _navigationService;
    private readonly IWebSocketService _webSocketService;
    private readonly IActionExecutorService _actionExecutor;

    public ShellPage()
    {
        _navigationService = App.GetService<NavigationService>();
        _webSocketService = App.GetService<IWebSocketService>();
        _actionExecutor = App.GetService<IActionExecutorService>();

        InitializeComponent();

        // Set the content frame for navigation service
        _navigationService.Frame = ContentFrame;

        // Wire up WebSocket command handling
        _webSocketService.CommandReceived += async (s, command) =>
        {
            var response = await _actionExecutor.ExecuteAsync(command);
            await _webSocketService.SendResponseAsync(response);
        };

        _webSocketService.ConnectionStateChanged += (s, connected) =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                ConnectionIcon.Glyph = connected ? "\uF385" : "\uF384";
            });
        };

        // Auto-connect WebSocket
        _ = ConnectWebSocketAsync();
    }

    private async Task ConnectWebSocketAsync()
    {
        var authService = App.GetService<IAuthService>();
        if (authService.IsAuthenticated)
        {
            var token = authService.CurrentUser?.Token ?? "";
            var wsUrl = Helpers.SecureStorage.GetWsBaseUrl();
            try
            {
                await _webSocketService.ConnectAsync(wsUrl, token);
            }
            catch
            {
                // WebSocket connection is optional, don't crash the app
            }
        }
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        // Navigate to Dashboard by default
        ContentFrame.Navigate(typeof(DashboardPage));
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            switch (tag)
            {
                case "DashboardPage":
                    ContentFrame.Navigate(typeof(DashboardPage));
                    break;
                case "DevicePairingPage":
                    ContentFrame.Navigate(typeof(DevicePairingPage));
                    break;
                case "DevPage":
                    ContentFrame.Navigate(typeof(DevPage));
                    break;
            }
        }
    }
}
