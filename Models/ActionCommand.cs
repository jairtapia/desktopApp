using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DesktopAssistant.Models;

/// <summary>
/// Represents a command received via WebSocket to execute on the system.
/// </summary>
public class ActionCommand
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("target")]
    public string? Target { get; set; }

    [JsonPropertyName("params")]
    public Dictionary<string, object>? Parameters { get; set; }

    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Known action types that can be executed.
/// </summary>
public static class ActionTypes
{
    public const string OpenApp = "open_app";
    public const string CloseApp = "close_app";
    public const string SplitScreenLeft = "split_screen_left";
    public const string SplitScreenRight = "split_screen_right";
    public const string VolumeUp = "volume_up";
    public const string VolumeDown = "volume_down";
    public const string VolumeMute = "volume_mute";
    public const string BrightnessUp = "brightness_up";
    public const string BrightnessDown = "brightness_down";
    public const string LockScreen = "lock_screen";
    public const string Screenshot = "screenshot";
    public const string MinimizeAll = "minimize_all";
    public const string MaximizeWindow = "maximize_window";
    public const string MinimizeWindow = "minimize_window";
    public const string ListApps = "list_apps";
    public const string ListRunning = "list_running";
}

/// <summary>
/// Response sent back through WebSocket after executing an action.
/// </summary>
public class ActionResponse
{
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
