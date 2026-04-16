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

    public ObservableCollection<AppInfo> RunningApps { get; } = new();
    public ObservableCollection<string> RecentActions { get; } = new();

    public DashboardViewModel(
        IAppScannerService appScanner,
        IWebSocketService webSocketService,
        IAuthService authService,
        ISyncService syncService,
        NavigationService navigationService)
    {
        _appScanner = appScanner;
        _webSocketService = webSocketService;
        _authService = authService;
        _syncService = syncService;
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
        {
            RunningApps.Add(app);
        }
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
        var data = GenerateSyncData();
        await _syncService.SendSyncDataAsync(data);
    }

    private List<SyncCategory> GenerateSyncData()
    {
        // This is a partial implementation of the long structure provided by the user
        return new List<SyncCategory>
        {
            new SyncCategory 
            { 
                Id = "system", Name = "SYSTEM MONITOR", Color = "#64748B", Icon = "Desktop",
                Shortcuts = new List<SyncShortcut>
                {
                    new SyncShortcut { Id = "SYS_CPU", Label = "PROCESADOR", Icon = "Cpu", Size = "big", Subtitle = "Intel Core i9 · 13th Gen", ProgressValue = 42 },
                    new SyncShortcut { Id = "SYS_RAM", Label = "MEMORIA RAM", Icon = "Memory", Size = "tall", Detail = "DDR5 · 32 GB total" }
                }
            },
            new SyncCategory
            {
                Id = "spotify", Name = "SPOTIFY", Color = "#1DB954", Icon = "MusicNotes",
                Shortcuts = new List<SyncShortcut>
                {
                    new SyncShortcut { Id = "SP_NOW", Label = "NOW PLAYING", Icon = "MusicNotes", Size = "big", Subtitle = "Tame Impala — The Less I Know", ProgressValue = 62 }
                }
            }
        };
    }

    [RelayCommand]
    private void NavigateToSync()
    {
        _navigationService.Navigate(typeof(Views.DevicePairingPage));
    }
}
