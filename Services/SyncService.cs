using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DesktopAssistant.Models;

namespace DesktopAssistant.Services;

public class SyncService : ISyncService
{
    private readonly IWebSocketService _webSocketService;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public SyncService(IWebSocketService webSocketService)
    {
        _webSocketService = webSocketService;
    }

    public async Task SendSyncDataAsync(List<SyncCategory> data)
    {
        if (!_webSocketService.IsConnected) return;

        var message = new { type = "sync_data", payload = data };
        var json = JsonSerializer.Serialize(message, SerializerOptions);
        await _webSocketService.SendMessageAsync(json);
    }
}

public interface ISyncService
{
    Task SendSyncDataAsync(List<SyncCategory> data);
}
