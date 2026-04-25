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
    public const string SetVolume = "set_volume";
    public const string MediaPlay    = "media_play";
    public const string MediaPause   = "media_pause";
    public const string MediaNext    = "media_next";
    public const string MediaPrev    = "media_prev";
    public const string MediaStop    = "media_stop";
    public const string MediaShuffle = "media_shuffle";
    public const string SetFanSpeed = "set_fan_speed";
    public const string OpenUrl = "open_url";
    public const string SendNotification = "send_notification";
    public const string RunScript = "run_script";
    public const string KillProcess = "kill_process";
    public const string SetClipboard = "set_clipboard";
    public const string TypeText = "type_text";
    public const string PressKey = "press_key";
    public const string MouseClick = "mouse_click";
    public const string ScrollPage = "scroll_page";
    public const string ZoomBrowser = "zoom_browser";
    public const string ToggleDarkMode = "toggle_dark_mode";
    public const string SleepDisplay = "sleep_display";
    public const string WakeDisplay = "wake_display";
    public const string Shutdown           = "shutdown";
    public const string Restart            = "restart";
    public const string TaskView           = "task_view";
    public const string CloseWindow        = "close_window";
    public const string VirtualDesktopNext = "virtual_desktop_next";
    public const string VirtualDesktopPrev = "virtual_desktop_prev";
    public const string Copy               = "copy";
    public const string Paste              = "paste";
    public const string Undo               = "undo";
    public const string NewTab             = "new_tab";
    public const string Save               = "save";
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
