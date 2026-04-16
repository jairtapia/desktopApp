using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DesktopAssistant.Models;

namespace DesktopAssistant.Services;

/// <summary>
/// Executes system actions like opening/closing apps, volume, brightness, etc.
/// Uses Windows APIs and simulated key presses.
/// </summary>
public class ActionExecutorService : IActionExecutorService
{
    private readonly IAppScannerService _appScanner;

    #region Win32 Imports

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool LockWorkStation();

    private const byte VK_VOLUME_UP = 0xAF;
    private const byte VK_VOLUME_DOWN = 0xAE;
    private const byte VK_VOLUME_MUTE = 0xAD;
    private const byte VK_LWIN = 0x5B;
    private const byte VK_LEFT = 0x25;
    private const byte VK_RIGHT = 0x27;
    private const byte VK_UP = 0x26;
    private const byte VK_DOWN = 0x28;
    private const byte VK_D = 0x44;
    private const byte VK_SNAPSHOT = 0x2C;
    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const int SW_MINIMIZE = 6;
    private const int SW_MAXIMIZE = 3;
    private const int SW_RESTORE = 9;
    private const uint WM_CLOSE = 0x0010;

    #endregion

    public ActionExecutorService(IAppScannerService appScanner)
    {
        _appScanner = appScanner;
    }

    public async Task<ActionResponse> ExecuteAsync(ActionCommand command)
    {
        try
        {
            return command.Action switch
            {
                ActionTypes.OpenApp => await OpenAppAsync(command),
                ActionTypes.CloseApp => await CloseAppAsync(command),
                ActionTypes.SplitScreenLeft => await SplitScreenAsync(true),
                ActionTypes.SplitScreenRight => await SplitScreenAsync(false),
                ActionTypes.VolumeUp => VolumeControl(VK_VOLUME_UP, command),
                ActionTypes.VolumeDown => VolumeControl(VK_VOLUME_DOWN, command),
                ActionTypes.VolumeMute => VolumeControl(VK_VOLUME_MUTE, command),
                ActionTypes.BrightnessUp => await AdjustBrightnessAsync(true),
                ActionTypes.BrightnessDown => await AdjustBrightnessAsync(false),
                ActionTypes.LockScreen => LockScreenAction(),
                ActionTypes.Screenshot => await TakeScreenshotAsync(),
                ActionTypes.MinimizeAll => MinimizeAllWindows(),
                ActionTypes.MaximizeWindow => MaximizeWindowAction(command),
                ActionTypes.MinimizeWindow => MinimizeWindowAction(command),
                ActionTypes.ListApps => await ListAppsAsync(),
                ActionTypes.ListRunning => await ListRunningAsync(),
                // ── Remote Mappings (Android MockData IDs) ──
                "SYS_CPU" => new ActionResponse { Success = true, Message = "CPU Telemetry updated" },
                "SP_VOL" => VolumeControl(VK_VOLUME_DOWN, command), // Example mapping
                "BR_ZOOM" => await AdjustBrightnessAsync(true), // Brightness as proxy for zoom
                "SYS_UPT" => LockScreenAction(), // Lock as proxy for testing 
                _ => new ActionResponse
                {
                    RequestId = command.RequestId,
                    Success = false,
                    Message = $"Unknown action: {command.Action}"
                }
            };
        }
        catch (Exception ex)
        {
            return new ActionResponse
            {
                RequestId = command.RequestId,
                Success = false,
                Message = $"Error executing action: {ex.Message}"
            };
        }
    }

    private async Task<ActionResponse> OpenAppAsync(ActionCommand command)
    {
        var target = command.Target;
        if (string.IsNullOrEmpty(target))
        {
            return new ActionResponse
            {
                RequestId = command.RequestId,
                Success = false,
                Message = "No target application specified"
            };
        }

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true
                }
            };
            process.Start();
            await Task.Delay(500); // Wait for app to start

            return new ActionResponse
            {
                RequestId = command.RequestId,
                Success = true,
                Message = $"Opened: {target}"
            };
        }
        catch (Exception ex)
        {
            return new ActionResponse
            {
                RequestId = command.RequestId,
                Success = false,
                Message = $"Failed to open {target}: {ex.Message}"
            };
        }
    }

    private async Task<ActionResponse> CloseAppAsync(ActionCommand command)
    {
        var target = command.Target;
        if (string.IsNullOrEmpty(target))
        {
            return new ActionResponse
            {
                RequestId = command.RequestId,
                Success = false,
                Message = "No target application specified"
            };
        }

        return await Task.Run(() =>
        {
            var processes = Process.GetProcessesByName(target);
            if (processes.Length == 0)
            {
                // Try matching by window title
                processes = Process.GetProcesses()
                    .Where(p => p.MainWindowTitle.Contains(target, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }

            if (processes.Length == 0)
            {
                return new ActionResponse
                {
                    RequestId = command.RequestId,
                    Success = false,
                    Message = $"No running process found: {target}"
                };
            }

            foreach (var process in processes)
            {
                try
                {
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        PostMessage(process.MainWindowHandle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    }
                    else
                    {
                        process.Kill();
                    }
                }
                catch { }
            }

            return new ActionResponse
            {
                RequestId = command.RequestId,
                Success = true,
                Message = $"Closed: {target} ({processes.Length} instance(s))"
            };
        });
    }

    private async Task<ActionResponse> SplitScreenAsync(bool left)
    {
        // Simulate Win + Left/Right arrow for split screen
        byte arrowKey = left ? VK_LEFT : VK_RIGHT;

        keybd_event(VK_LWIN, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        keybd_event(arrowKey, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        await Task.Delay(50);
        keybd_event(arrowKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

        return new ActionResponse
        {
            Success = true,
            Message = $"Split screen {(left ? "left" : "right")}"
        };
    }

    private ActionResponse VolumeControl(byte volumeKey, ActionCommand command)
    {
        int steps = 1;
        if (command.Parameters?.TryGetValue("steps", out var stepsObj) == true)
        {
            if (stepsObj is int s) steps = s;
            else if (int.TryParse(stepsObj?.ToString(), out var parsed)) steps = parsed;
        }

        for (int i = 0; i < steps; i++)
        {
            keybd_event(volumeKey, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            keybd_event(volumeKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        var actionName = volumeKey switch
        {
            VK_VOLUME_UP => "Volume Up",
            VK_VOLUME_DOWN => "Volume Down",
            VK_VOLUME_MUTE => "Volume Mute Toggle",
            _ => "Volume"
        };

        return new ActionResponse
        {
            RequestId = command.RequestId,
            Success = true,
            Message = $"{actionName} ({steps} steps)"
        };
    }

    private async Task<ActionResponse> AdjustBrightnessAsync(bool increase)
    {
        // Use PowerShell to adjust brightness via WMI
        var direction = increase ? "+" : "-";
        var script = increase
            ? "(Get-CimInstance -Namespace root/WMI -ClassName WmiMonitorBrightnessMethods).WmiSetBrightness(1, [Math]::Min(100, (Get-CimInstance -Namespace root/WMI -ClassName WmiMonitorBrightness).CurrentBrightness + 10))"
            : "(Get-CimInstance -Namespace root/WMI -ClassName WmiMonitorBrightnessMethods).WmiSetBrightness(1, [Math]::Max(0, (Get-CimInstance -Namespace root/WMI -ClassName WmiMonitorBrightness).CurrentBrightness - 10))";

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-Command \"{script}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            await process.WaitForExitAsync();

            return new ActionResponse
            {
                Success = true,
                Message = $"Brightness {(increase ? "increased" : "decreased")}"
            };
        }
        catch (Exception ex)
        {
            return new ActionResponse
            {
                Success = false,
                Message = $"Brightness adjustment failed: {ex.Message}"
            };
        }
    }

    private ActionResponse LockScreenAction()
    {
        LockWorkStation();
        return new ActionResponse
        {
            Success = true,
            Message = "Screen locked"
        };
    }

    private async Task<ActionResponse> TakeScreenshotAsync()
    {
        // Simulate PrintScreen key
        keybd_event(VK_SNAPSHOT, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        await Task.Delay(50);
        keybd_event(VK_SNAPSHOT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

        return new ActionResponse
        {
            Success = true,
            Message = "Screenshot taken (copied to clipboard)"
        };
    }

    private ActionResponse MinimizeAllWindows()
    {
        // Simulate Win + D
        keybd_event(VK_LWIN, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        keybd_event(VK_D, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        keybd_event(VK_D, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

        return new ActionResponse
        {
            Success = true,
            Message = "All windows minimized"
        };
    }

    private ActionResponse MaximizeWindowAction(ActionCommand command)
    {
        if (command.Parameters?.TryGetValue("processId", out var pidObj) == true
            && int.TryParse(pidObj?.ToString(), out var pid))
        {
            try
            {
                var process = Process.GetProcessById(pid);
                ShowWindow(process.MainWindowHandle, SW_MAXIMIZE);
                SetForegroundWindow(process.MainWindowHandle);
                return new ActionResponse
                {
                    RequestId = command.RequestId,
                    Success = true,
                    Message = $"Maximized window: {process.ProcessName}"
                };
            }
            catch (Exception ex)
            {
                return new ActionResponse
                {
                    RequestId = command.RequestId,
                    Success = false,
                    Message = $"Failed to maximize: {ex.Message}"
                };
            }
        }

        return new ActionResponse
        {
            RequestId = command.RequestId,
            Success = false,
            Message = "No processId specified"
        };
    }

    private ActionResponse MinimizeWindowAction(ActionCommand command)
    {
        if (command.Parameters?.TryGetValue("processId", out var pidObj) == true
            && int.TryParse(pidObj?.ToString(), out var pid))
        {
            try
            {
                var process = Process.GetProcessById(pid);
                ShowWindow(process.MainWindowHandle, SW_MINIMIZE);
                return new ActionResponse
                {
                    RequestId = command.RequestId,
                    Success = true,
                    Message = $"Minimized window: {process.ProcessName}"
                };
            }
            catch (Exception ex)
            {
                return new ActionResponse
                {
                    RequestId = command.RequestId,
                    Success = false,
                    Message = $"Failed to minimize: {ex.Message}"
                };
            }
        }

        return new ActionResponse
        {
            RequestId = command.RequestId,
            Success = false,
            Message = "No processId specified"
        };
    }

    private async Task<ActionResponse> ListAppsAsync()
    {
        var apps = await _appScanner.ScanInstalledAppsAsync();
        return new ActionResponse
        {
            Success = true,
            Message = $"Found {apps.Count} installed apps",
            Data = apps.Select(a => new { a.Name, a.ExecutablePath, a.Publisher }).ToList()
        };
    }

    private async Task<ActionResponse> ListRunningAsync()
    {
        var apps = await _appScanner.GetRunningAppsAsync();
        return new ActionResponse
        {
            Success = true,
            Message = $"Found {apps.Count} running apps",
            Data = apps.Select(a => new { a.Name, a.ProcessId, a.WindowTitle }).ToList()
        };
    }
}
