using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopAssistant.Services;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.UI.Dispatching;
using Windows.ApplicationModel.DataTransfer;

namespace DesktopAssistant.ViewModels;

public partial class DevViewModel : ObservableObject
{
    private readonly IWebSocketService _webSocketService;
    private readonly SyncDataBuilderService _syncBuilder;
    private readonly ActiveAppMonitorService _activeAppMonitor;
    private readonly IAppScannerService _appScanner;
    private readonly DispatcherQueue _dispatcherQueue;

    private static readonly JsonSerializerOptions PrettyOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    [ObservableProperty]
    public partial ObservableCollection<string> Logs { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotScanning))]
    public partial bool IsScanning { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasJson))]
    public partial string ScanJson { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ScanStatus { get; set; } = "Sin escanear";

    [ObservableProperty]
    public partial string ActiveAppName { get; set; } = "—";

    [ObservableProperty]
    public partial string ActiveAppTitle { get; set; } = "—";

    [ObservableProperty]
    public partial string ActiveAppPath { get; set; } = "—";

    [ObservableProperty]
    public partial string ActiveAppPid { get; set; } = "—";

    [ObservableProperty]
    public partial ObservableCollection<string> AppHistory { get; set; } = new();

    public bool IsNotScanning => !IsScanning;
    public bool HasJson => !string.IsNullOrEmpty(ScanJson);

    public DevViewModel(IWebSocketService webSocketService, SyncDataBuilderService syncBuilder,
        ActiveAppMonitorService activeAppMonitor, IAppScannerService appScanner)
    {
        _webSocketService = webSocketService;
        _syncBuilder = syncBuilder;
        _activeAppMonitor = activeAppMonitor;
        _appScanner = appScanner;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        foreach (var log in _webSocketService.MessageLogs)
            Logs.Add(log);

        _webSocketService.RawMessageReceived += OnRawMessageReceived;
        _activeAppMonitor.ActiveAppChanged += OnActiveAppChanged;

        var currentApp = _activeAppMonitor.CurrentActiveApp ?? _appScanner.GetForegroundApp();
        if (currentApp != null)
        {
            ApplyActiveAppSnapshot(currentApp);
        }
    }

    private void OnRawMessageReceived(object? sender, string log)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            Logs.Insert(0, log);
            if (Logs.Count > 100) Logs.RemoveAt(100);
        });
    }

    private void OnActiveAppChanged(object? sender, DesktopAssistant.Models.AppInfo app)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            ApplyActiveAppSnapshot(app);
            var entry = $"[{DateTime.Now:HH:mm:ss}]  {app.Name}  —  {app.WindowTitle}";
            AppHistory.Insert(0, entry);
            if (AppHistory.Count > 200) AppHistory.RemoveAt(200);
        });
    }

    private void ApplyActiveAppSnapshot(DesktopAssistant.Models.AppInfo app)
    {
        ActiveAppName = string.IsNullOrWhiteSpace(app.Name) ? "—" : app.Name;
        ActiveAppTitle = string.IsNullOrWhiteSpace(app.WindowTitle) ? "—" : app.WindowTitle;
        ActiveAppPath = string.IsNullOrWhiteSpace(app.ExecutablePath) ? "—" : app.ExecutablePath;
        ActiveAppPid = app.ProcessId?.ToString() ?? "—";
    }

    [RelayCommand]
    private void ClearAppHistory() => AppHistory.Clear();

    [RelayCommand]
    private void ClearLogs()
    {
        _webSocketService.MessageLogs.Clear();
        Logs.Clear();
    }

    [RelayCommand]
    private async Task RunScan()
    {
        IsScanning = true;
        ScanStatus = "Escaneando aplicaciones instaladas…";
        ScanJson = string.Empty;

        try
        {
            var data = await _syncBuilder.BuildAsync();
            ScanJson = JsonSerializer.Serialize(data, PrettyOptions);
            ScanStatus = $"{data.Count} categorías · {data.Sum(c => c.Shortcuts.Count)} shortcuts";
        }
        catch (Exception ex)
        {
            ScanJson = $"Error: {ex.Message}";
            ScanStatus = "Error durante el escaneo";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private void CopyJson()
    {
        var package = new DataPackage();
        package.SetText(ScanJson);
        Clipboard.SetContent(package);
        ScanStatus = "JSON copiado al portapapeles";
    }

    [RelayCommand]
    private async Task SimulateFocusBrowser()
    {
        await _webSocketService.SendMessageAsync("{\"type\": \"app_focused\", \"payload\": {\"name\": \"Chrome\", \"title\": \"Google Search\", \"pid\": 1001}}");
    }

    [RelayCommand]
    private async Task SimulateFocusWord()
    {
        await _webSocketService.SendMessageAsync("{\"type\": \"app_focused\", \"payload\": {\"name\": \"Word\", \"title\": \"Document1.docx\", \"pid\": 2001}}");
    }

    [RelayCommand]
    private async Task SimulateFocusExcel()
    {
        await _webSocketService.SendMessageAsync("{\"type\": \"app_focused\", \"payload\": {\"name\": \"Excel\", \"title\": \"Book1.xlsx\", \"pid\": 3001}}");
    }

    [RelayCommand]
    private async Task SimulateFocusSpotify()
    {
        await _webSocketService.SendMessageAsync("{\"type\": \"app_focused\", \"payload\": {\"name\": \"Spotify\", \"title\": \"Now Playing\", \"pid\": 4001}}");
    }

    [RelayCommand]
    private async Task SimulateFocusExplorer()
    {
        await _webSocketService.SendMessageAsync("{\"type\": \"app_focused\", \"payload\": {\"name\": \"Explorer\", \"title\": \"Downloads\", \"pid\": 5001}}");
    }

    [RelayCommand]
    private async Task SimulateFocusSystem()
    {
        await _webSocketService.SendMessageAsync("{\"type\": \"app_focused\", \"payload\": {\"name\": \"System\", \"title\": \"System Monitor\", \"pid\": 6001}}");
    }

    [RelayCommand]
    private async Task SimulateFocusXTools()
    {
        await _webSocketService.SendMessageAsync("{\"type\": \"app_focused\", \"payload\": {\"name\": \"XTools\", \"title\": \"X-Tools Dashboard\", \"pid\": 7001}}");
    }

    [RelayCommand]
    private async Task SimulateNLP()
    {
        await _webSocketService.SendMessageAsync("{\"type\": \"nlp_input\", \"payload\": {\"text\": \"Abre el bloc de notas y sube el volumen\"}}");
    }
}
