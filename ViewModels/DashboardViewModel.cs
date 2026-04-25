using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopAssistant.Models;
using DesktopAssistant.Services;

namespace DesktopAssistant.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IAppScannerService _appScanner;
    private readonly IWebSocketService _webSocketService;
    private readonly IAuthService _authService;
    private readonly ISyncService _syncService;
    private readonly SyncDataBuilderService _syncBuilder;
    private readonly NavigationService _navigationService;

    [ObservableProperty]
    public partial bool IsConnected { get; set; }

    [ObservableProperty]
    public partial string ConnectionStatus { get; set; } = "Disconnected";

    [ObservableProperty]
    public partial string UserName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int InstalledAppsCount { get; set; }

    [ObservableProperty]
    public partial int RunningAppsCount { get; set; }

    [ObservableProperty]
    public partial int CommandsExecuted { get; set; }

    [ObservableProperty]
    public partial bool IsScanning { get; set; }

    [ObservableProperty]
    public partial bool IsSyncing { get; set; }

    public ObservableCollection<AppInfo> RunningApps { get; } = new();
    public ObservableCollection<string> RecentActions { get; } = new();

    public DashboardViewModel(
        IAppScannerService appScanner,
        IWebSocketService webSocketService,
        IAuthService authService,
        ISyncService syncService,
        SyncDataBuilderService syncBuilder,
        NavigationService navigationService)
    {
        _appScanner = appScanner;
        _webSocketService = webSocketService;
        _authService = authService;
        _syncService = syncService;
        _syncBuilder = syncBuilder;
        _navigationService = navigationService;

        UserName = _authService.CurrentUser?.Name ?? "User";
        _webSocketService.ConnectionStateChanged += OnConnectionStateChanged;
    }

    private void OnConnectionStateChanged(object? sender, bool connected)
    {
        IsConnected = connected;
        ConnectionStatus = connected ? "Connected" : "Disconnected";
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsScanning = true;

        var running = await _appScanner.GetRunningAppsAsync();
        RunningApps.Clear();
        foreach (var app in running)
            RunningApps.Add(app);
        RunningAppsCount = running.Count;

        var installed = await _appScanner.ScanInstalledAppsAsync();
        InstalledAppsCount = installed.Count;

        IsScanning = false;
    }

    [RelayCommand]
    private async Task ConnectWebSocketAsync()
    {
        if (_webSocketService.IsConnected)
        {
            await _webSocketService.DisconnectAsync();
        }
        else
        {
            var token = _authService.CurrentUser?.Token ?? "";
            var wsUrl = Helpers.SecureStorage.GetWsBaseUrl();
            await _webSocketService.ConnectAsync(wsUrl, token);
        }
    }

    [RelayCommand]
    private async Task RefreshAppsAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        if (IsSyncing) return;
        IsSyncing = true;
        try
        {
            var data = await _syncBuilder.BuildAsync();
            await _syncService.SendSyncDataAsync(data);
        }
        finally
        {
            IsSyncing = false;
        }
    }

    [RelayCommand]
    private void NavigateToSync()
    {
        _navigationService.Navigate(typeof(Views.DevicePairingPage));
    }
}
