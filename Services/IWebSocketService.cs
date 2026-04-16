using System;
using System.Threading.Tasks;
using DesktopAssistant.Models;

namespace DesktopAssistant.Services;

/// <summary>
/// WebSocket client service for real-time communication.
/// </summary>
public interface IWebSocketService
{
    Task ConnectAsync(string url, string authToken);
    Task DisconnectAsync();
    Task SendMessageAsync(string message);
    Task SendResponseAsync(ActionResponse response);
    bool IsConnected { get; }
    List<string> MessageLogs { get; }
    event EventHandler<ActionCommand>? CommandReceived;
    event EventHandler<bool>? ConnectionStateChanged;
    event EventHandler<string>? ErrorOccurred;
    event EventHandler<string>? RawMessageReceived;
}
