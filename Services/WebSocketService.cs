using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DesktopAssistant.Models;

namespace DesktopAssistant.Services;

/// <summary>
/// WebSocket client service implementation with auto-reconnect.
/// </summary>
public class WebSocketService : IWebSocketService, IDisposable
{
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cts;
    private string _url = string.Empty;
    private string _authToken = string.Empty;
    private bool _isReconnecting;
    private int _reconnectAttempts;
    private const int MaxReconnectAttempts = 10;
    private const int ReconnectDelayMs = 3000;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public bool IsConnected => _webSocket?.State == WebSocketState.Open;
    public event EventHandler<ActionCommand>? CommandReceived;
    public event EventHandler<bool>? ConnectionStateChanged;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler<string>? RawMessageReceived;

    public List<string> MessageLogs { get; } = new();

    private void LogMessage(string message, bool isIncoming)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logEntry = $"[{timestamp}] {(isIncoming ? "IN" : "OUT")}: {message}";
        MessageLogs.Insert(0, logEntry);
        if (MessageLogs.Count > 100) MessageLogs.RemoveAt(100);
        RawMessageReceived?.Invoke(this, logEntry);
    }

    public async Task ConnectAsync(string url, string authToken)
    {
        _url = url;
        _authToken = authToken;
        _reconnectAttempts = 0;

        await InternalConnectAsync();
    }

    private async Task InternalConnectAsync()
    {
        try
        {
            _cts?.Cancel();
            _cts?.Dispose();

            if (_webSocket is { State: WebSocketState.Open })
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconnecting", CancellationToken.None);
            }

            _webSocket?.Dispose();
            _webSocket = new ClientWebSocket();
            _cts = new CancellationTokenSource();

            // Add auth token as query parameter (FastAPI server expectation)
            var connectionUrl = _url;
            if (!string.IsNullOrEmpty(_authToken))
            {
                var separator = connectionUrl.Contains('?') ? "&" : "?";
                connectionUrl += $"{separator}token={_authToken}";
            }

            var uri = new Uri(connectionUrl);
            await _webSocket.ConnectAsync(uri, _cts.Token);

            _reconnectAttempts = 0;
            ConnectionStateChanged?.Invoke(this, true);

            // Start receiving messages
            _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Connection failed: {ex.Message}");
            ConnectionStateChanged?.Invoke(this, false);
            await TryReconnectAsync();
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        var buffer = new byte[4096];

        try
        {
            while (_webSocket?.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                var messageBuffer = new StringBuilder();
                WebSocketReceiveResult result;

                do
                {
                    result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    messageBuffer.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    ConnectionStateChanged?.Invoke(this, false);
                    await TryReconnectAsync();
                    return;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var text = messageBuffer.ToString();
                    LogMessage(text, true);
                    ProcessMessage(text);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Receive error: {ex.Message}");
            ConnectionStateChanged?.Invoke(this, false);
            await TryReconnectAsync();
        }
    }

    private void ProcessMessage(string message)
    {
        try
        {
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;
            
            if (!root.TryGetProperty("type", out var typeProp)) return;
            var type = typeProp.GetString();

            // Ignore system messages and errors to avoid loops
            if (type == "connected" || type == "pong" || type == "error")
            {
                LogMessage($"System message: {type}", true);
                return;
            }

            // Handle incoming commands (plans or remote commands from Android)
            if (type == "action_plan" || type == "action" || type == "remote_command")
            {
                var jsonToParse = message;
                
                // If it's a remote command, the actual ActionCommand is in the payload
                if (type == "remote_command")
                {
                    if (root.TryGetProperty("payload", out var payloadProp))
                    {
                        jsonToParse = payloadProp.GetRawText();
                    }
                }

                var command = JsonSerializer.Deserialize<ActionCommand>(jsonToParse, _jsonOptions);
                if (command != null && !string.IsNullOrEmpty(command.Action))
                {
                    CommandReceived?.Invoke(this, command);
                }
            }
            else if (type == "telemetry_update")
            {
                LogMessage("Telemetry update received", true);
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Message parse error: {ex.Message}");
        }
    }

    public async Task SendMessageAsync(string message)
    {
        if (_webSocket?.State != WebSocketState.Open)
        {
            LogMessage($"[OFFLINE] Cannot send: {message}", false);
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(message);
        LogMessage(message, false);
        await _webSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            _cts?.Token ?? CancellationToken.None
        );
    }

    public async Task SendResponseAsync(ActionResponse response)
    {
        var json = JsonSerializer.Serialize(response, _jsonOptions);
        await SendMessageAsync(json);
    }

    public async Task DisconnectAsync()
    {
        _isReconnecting = false;
        _cts?.Cancel();

        if (_webSocket is { State: WebSocketState.Open })
        {
            try
            {
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Client disconnecting",
                    CancellationToken.None
                );
            }
            catch { }
        }

        ConnectionStateChanged?.Invoke(this, false);
    }

    private async Task TryReconnectAsync()
    {
        if (_isReconnecting || _reconnectAttempts >= MaxReconnectAttempts) return;

        _isReconnecting = true;
        _reconnectAttempts++;

        var delay = ReconnectDelayMs * Math.Min(_reconnectAttempts, 5);
        await Task.Delay(delay);

        _isReconnecting = false;
        await InternalConnectAsync();
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _webSocket?.Dispose();
    }
}
