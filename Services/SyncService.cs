using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DesktopAssistant.Models;
using System.Text.Json;

namespace DesktopAssistant.Services;

public class SyncService : ISyncService
{
    private readonly IWebSocketService _webSocketService;

    public SyncService(IWebSocketService webSocketService)
    {
        _webSocketService = webSocketService;
    }

    public async Task SendSyncDataAsync(List<SyncCategory> data)
    {
        if (!_webSocketService.IsConnected) return;

        var message = new
        {
            type = "sync_data",
            payload = data
        };

        var json = JsonSerializer.Serialize(message);
        await _webSocketService.SendMessageAsync(json);
    }
}

public interface ISyncService
{
    Task SendSyncDataAsync(List<SyncCategory> data);
}
