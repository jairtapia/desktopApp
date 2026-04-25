using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DesktopAssistant.Models;

namespace DesktopAssistant.Services;

public class ActiveAppMonitorService : IDisposable
{
    private readonly IAppScannerService _appScanner;
    private readonly IWebSocketService _webSocketService;
    private readonly ISyncService _syncService;
    private readonly SyncDataBuilderService _syncBuilder;
    private CancellationTokenSource? _cts;
    private string? _lastProcessName;
    private DateTime _lastSyncAt = DateTime.MinValue;
    private bool _syncInFlight;

    private static readonly TimeSpan SyncInterval = TimeSpan.FromSeconds(60);

    public event EventHandler<AppInfo>? ActiveAppChanged;
    public AppInfo? CurrentActiveApp { get; private set; }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public ActiveAppMonitorService(
        IAppScannerService appScanner,
        IWebSocketService webSocketService,
        ISyncService syncService,
        SyncDataBuilderService syncBuilder)
    {
        _appScanner = appScanner;
        _webSocketService = webSocketService;
        _syncService = syncService;
        _syncBuilder = syncBuilder;
        _webSocketService.ConnectionStateChanged += OnConnectionStateChanged;

        var initialApp = _appScanner.GetForegroundApp();
        if (initialApp != null)
        {
            CurrentActiveApp = SyncAppCatalog.NormalizeAppInfo(initialApp);
        }

        StartMonitoring();
    }

    private void OnConnectionStateChanged(object? sender, bool connected)
    {
        if (connected)
        {
            _ = PublishSyncSnapshotAsync(force: true);
        }
    }

    public void StartMonitoring()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            return;
        }

        _lastSyncAt = DateTime.MinValue;
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => MonitorLoopAsync(_cts.Token));
    }

    public void StopMonitoring()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _lastProcessName = null;
    }

    private async Task MonitorLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var detectedApp = _appScanner.GetForegroundApp();
                var current = detectedApp?.ProcessId.ToString() + detectedApp?.WindowTitle;

                if (detectedApp != null && current != _lastProcessName)
                {
                    var app = SyncAppCatalog.NormalizeAppInfo(detectedApp);
                    _lastProcessName = current;
                    CurrentActiveApp = app;
                    ActiveAppChanged?.Invoke(this, app);

                    if (_webSocketService.IsConnected)
                    {
                        var message = new
                        {
                            type = "app_focused",
                            payload = new
                            {
                                name = app.Name,
                                appName = app.Name,
                                title = app.WindowTitle,
                                windowTitle = app.WindowTitle,
                                processId = app.ProcessId,
                                pid = app.ProcessId,
                                path = app.ExecutablePath,
                            }
                        };
                        var json = JsonSerializer.Serialize(message, _jsonOptions);
                        await _webSocketService.SendMessageAsync(json);
                    }
                }

                if (_webSocketService.IsConnected)
                {
                    await PublishSyncSnapshotAsync();
                }
            }
            catch { }

            await Task.Delay(1000, token).ConfigureAwait(false);
        }
    }

    private async Task PublishSyncSnapshotAsync(bool force = false)
    {
        if (!_webSocketService.IsConnected || _syncInFlight)
        {
            return;
        }

        if (!force && DateTime.UtcNow - _lastSyncAt < SyncInterval)
        {
            return;
        }

        _syncInFlight = true;
        try
        {
            var data = await _syncBuilder.BuildAsync();
            await _syncService.SendSyncDataAsync(data);
            _lastSyncAt = DateTime.UtcNow;
        }
        catch
        {
            // Keep the monitor running even if sync publish fails.
        }
        finally
        {
            _syncInFlight = false;
        }
    }

    public void Dispose()
    {
        _webSocketService.ConnectionStateChanged -= OnConnectionStateChanged;
        StopMonitoring();
    }
}
