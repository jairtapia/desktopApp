using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using DesktopAssistant.Services;
using DesktopAssistant.ViewModels;

namespace DesktopAssistant;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// Configures dependency injection and manages the main window lifecycle.
/// </summary>
public partial class App : Application
{
    private Window? _window;
    private static IServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets a service from the DI container.
    /// </summary>
    public static T GetService<T>() where T : notnull
    {
        return _serviceProvider!.GetRequiredService<T>();
    }

    public App()
    {
        InitializeComponent();
        ConfigureServices();
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Services (singletons for stateful services)
        services.AddSingleton<NavigationService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IWebSocketService, WebSocketService>();
        services.AddSingleton<IAppScannerService, AppScannerService>();
        services.AddSingleton<IActionExecutorService, ActionExecutorService>();
        services.AddSingleton<IDevicePairingService, DevicePairingService>();
        services.AddSingleton<ISyncService, SyncService>();

        // ViewModels (transient - new instance per page)
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<TutorialViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<DevicePairingViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<DevViewModel>();

        _serviceProvider = services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
