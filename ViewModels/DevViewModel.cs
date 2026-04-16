using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopAssistant.Services;
using System.Collections.ObjectModel;
using Microsoft.UI.Dispatching;

namespace DesktopAssistant.ViewModels;

public partial class DevViewModel : ObservableObject
{
    private readonly IWebSocketService _webSocketService;
    private readonly DispatcherQueue _dispatcherQueue;

    [ObservableProperty]
    public partial ObservableCollection<string> Logs { get; set; } = new();

    public DevViewModel(IWebSocketService webSocketService)
    {
        _webSocketService = webSocketService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        // Initialize with existing logs
        foreach (var log in _webSocketService.MessageLogs)
        {
            Logs.Add(log);
        }

        // Subscribe to new messages
        _webSocketService.RawMessageReceived += OnRawMessageReceived;
    }

    private void OnRawMessageReceived(object? sender, string log)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            Logs.Insert(0, log);
            if (Logs.Count > 100) Logs.RemoveAt(100);
        });
    }

    [RelayCommand]
    private void ClearLogs()
    {
        _webSocketService.MessageLogs.Clear();
        Logs.Clear();
    }

    [RelayCommand]
    private async Task SimulateFocusBrowser()
    {
        var json = "{\"type\": \"app_focused\", \"payload\": {\"name\": \"Chrome\", \"title\": \"Google Search\", \"pid\": 1001}}";
        await _webSocketService.SendMessageAsync(json);
    }

    [RelayCommand]
    private async Task SimulateFocusWord()
    {
        var json = "{\"type\": \"app_focused\", \"payload\": {\"name\": \"Word\", \"title\": \"Document1.docx\", \"pid\": 2001}}";
        await _webSocketService.SendMessageAsync(json);
    }

    [RelayCommand]
    private async Task SimulateFocusExcel()
    {
        var json = "{\"type\": \"app_focused\", \"payload\": {\"name\": \"Excel\", \"title\": \"Book1.xlsx\", \"pid\": 3001}}";
        await _webSocketService.SendMessageAsync(json);
    }

    [RelayCommand]
    private async Task SimulateFocusSpotify()
    {
        var json = "{\"type\": \"app_focused\", \"payload\": {\"name\": \"Spotify\", \"title\": \"Now Playing\", \"pid\": 4001}}";
        await _webSocketService.SendMessageAsync(json);
    }

    [RelayCommand]
    private async Task SimulateFocusExplorer()
    {
        var json = "{\"type\": \"app_focused\", \"payload\": {\"name\": \"Explorer\", \"title\": \"Downloads\", \"pid\": 5001}}";
        await _webSocketService.SendMessageAsync(json);
    }

    [RelayCommand]
    private async Task SimulateFocusSystem()
    {
        var json = "{\"type\": \"app_focused\", \"payload\": {\"name\": \"System\", \"title\": \"System Monitor\", \"pid\": 6001}}";
        await _webSocketService.SendMessageAsync(json);
    }

    [RelayCommand]
    private async Task SimulateFocusXTools()
    {
        var json = "{\"type\": \"app_focused\", \"payload\": {\"name\": \"XTools\", \"title\": \"X-Tools Dashboard\", \"pid\": 7001}}";
        await _webSocketService.SendMessageAsync(json);
    }

    [RelayCommand]
    private async Task SimulateNLP()
    {
        var json = "{\"type\": \"nlp_input\", \"payload\": {\"text\": \"Abre el bloc de notas y sube el volumen\"}}";
        await _webSocketService.SendMessageAsync(json);
    }
}
