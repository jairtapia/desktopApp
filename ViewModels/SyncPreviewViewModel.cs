using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopAssistant.Models;
using DesktopAssistant.Services;

namespace DesktopAssistant.ViewModels;

public partial class SyncPreviewViewModel : ObservableObject
{
    private readonly SyncDataBuilderService _builder;
    private readonly ISyncService _syncService;
    private readonly IWebSocketService _webSocketService;

    public ObservableCollection<SyncCategory> Categories { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShortcutsForSelected))]
    public partial SyncCategory? SelectedCategory { get; set; }

    [ObservableProperty]
    public partial SyncShortcut? SelectedShortcut { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool SendToMobile { get; set; } = true;

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Sin datos  —  presiona Escanear.";

    [ObservableProperty]
    public partial bool IsConnected { get; set; }

    public bool IsNotLoading => !IsLoading;

    public ObservableCollection<SyncShortcut> ShortcutsForSelected { get; } = new();

    public SyncPreviewViewModel(
        SyncDataBuilderService builder,
        ISyncService syncService,
        IWebSocketService webSocketService)
    {
        _builder = builder;
        _syncService = syncService;
        _webSocketService = webSocketService;

        IsConnected = _webSocketService.IsConnected;
        _webSocketService.ConnectionStateChanged += (_, c) =>
        {
            IsConnected = c;
            OnPropertyChanged(nameof(IsConnected));
        };
    }

    partial void OnSelectedCategoryChanged(SyncCategory? value)
    {
        ShortcutsForSelected.Clear();
        if (value == null) return;
        foreach (var s in value.Shortcuts)
            ShortcutsForSelected.Add(s);
        SelectedShortcut = ShortcutsForSelected.FirstOrDefault();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        StatusText = "Escaneando sistema...";
        SelectedShortcut = null;
        SelectedCategory = null;
        Categories.Clear();
        ShortcutsForSelected.Clear();

        try
        {
            var data = await _builder.BuildAsync();
            foreach (var c in data)
                Categories.Add(c);

            SelectedCategory = Categories.FirstOrDefault();
            var total = data.Sum(c => c.Shortcuts.Count);
            StatusText = $"{data.Count} categorias  ·  {total} shortcuts";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SyncAsync()
    {
        if (!SendToMobile)
        {
            StatusText = "Sincronizacion desactivada  —  activa el checkbox para enviar.";
            return;
        }
        if (!IsConnected)
        {
            StatusText = "Sin conexion WebSocket  —  conecta primero desde el Dashboard.";
            return;
        }
        try
        {
            StatusText = "Enviando al celular...";
            await _syncService.SendSyncDataAsync(Categories.ToList());
            StatusText = $"Enviado  ·  {Categories.Count} categorias  ·  {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error al sincronizar: {ex.Message}";
        }
    }
}
