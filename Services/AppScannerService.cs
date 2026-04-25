using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DesktopAssistant.Models;
using Microsoft.Win32;

namespace DesktopAssistant.Services;

/// <summary>
/// Scans installed applications from registry and running processes.
/// </summary>
public class AppScannerService : IAppScannerService
{
    public event EventHandler<List<AppInfo>>? ScanCompleted;

    private static string CleanExecutablePath(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return string.Empty;
        }

        var exePath = rawPath;
        if (exePath.Contains(','))
        {
            exePath = exePath[..exePath.LastIndexOf(',')];
        }

        return exePath.Trim().Trim('"');
    }

    private static DateTime? ParseInstallDate(object? rawInstallDate)
    {
        if (rawInstallDate is not string value || string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParseExact(
            value,
            "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var installDate)
            ? installDate
            : null;
    }

    private static string GetProcessDisplayName(Process process)
    {
        try
        {
            var description = process.MainModule?.FileVersionInfo?.FileDescription;
            if (!string.IsNullOrWhiteSpace(description))
            {
                return description.Trim();
            }
        }
        catch
        {
            // Ignore inaccessible metadata and fall back to process name.
        }

        return process.ProcessName;
    }

    public async Task<List<AppInfo>> ScanInstalledAppsAsync()
    {
        return await Task.Run(() =>
        {
            var apps = new List<AppInfo>();

            var roots = new (RegistryKey hive, string path)[]
            {
                (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
                (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
                (Registry.CurrentUser,  @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
            };

            foreach (var (hive, path) in roots)
            {
                using var key = hive.OpenSubKey(path);
                if (key == null) continue;

                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    try
                    {
                        using var subKey = key.OpenSubKey(subKeyName);
                        if (subKey == null) continue;

                        var displayName = subKey.GetValue("DisplayName") as string;
                        if (string.IsNullOrWhiteSpace(displayName)) continue;

                        // Skip system components and updates
                        var systemComponent = subKey.GetValue("SystemComponent");
                        if (systemComponent is int sc && sc == 1) continue;

                        var parentKeyName = subKey.GetValue("ParentKeyName") as string;
                        if (!string.IsNullOrEmpty(parentKeyName)) continue;

                        var exePath = subKey.GetValue("DisplayIcon") as string
                            ?? subKey.GetValue("InstallLocation") as string
                            ?? "";

                        var app = new AppInfo
                        {
                            Name = displayName,
                            ExecutablePath = CleanExecutablePath(exePath),
                            Publisher = subKey.GetValue("Publisher") as string,
                            Version = subKey.GetValue("DisplayVersion") as string,
                            IconPath = subKey.GetValue("DisplayIcon") as string,
                            IsRunning = false,
                            InstallDate = ParseInstallDate(subKey.GetValue("InstallDate")),
                        };

                        // Avoid duplicates
                        if (!apps.Any(a => a.Name.Equals(app.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            apps.Add(app);
                        }
                    }
                    catch
                    {
                        // Skip entries we can't read
                    }
                }
            }

            return apps.OrderBy(a => a.Name).ToList();
        });
    }

    public async Task<List<AppInfo>> GetRunningAppsAsync()
    {
        return await Task.Run(() =>
        {
            var apps = new List<AppInfo>();
            var processes = Process.GetProcesses();

            foreach (var process in processes)
            {
                try
                {
                    // Only include processes with a main window (visible apps)
                    if (process.MainWindowHandle == IntPtr.Zero) continue;
                    if (string.IsNullOrWhiteSpace(process.MainWindowTitle)) continue;

                    string exePath;
                    try
                    {
                        exePath = process.MainModule?.FileName ?? "";
                    }
                    catch
                    {
                        exePath = process.ProcessName;
                    }

                    var app = new AppInfo
                    {
                        Name = GetProcessDisplayName(process),
                        ExecutablePath = exePath,
                        IsRunning = true,
                        ProcessId = process.Id,
                        WindowTitle = process.MainWindowTitle
                    };

                    if (!apps.Any(a => a.ProcessId == app.ProcessId))
                    {
                        apps.Add(app);
                    }
                }
                catch
                {
                    // Skip processes we can't access
                }
            }

            return apps.OrderBy(a => a.Name).ToList();
        });
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public AppInfo? GetForegroundApp()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return null;

            GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0) return null;

            var process = Process.GetProcessById((int)pid);
            if (string.IsNullOrWhiteSpace(process.MainWindowTitle)) return null;

            string exePath;
            try { exePath = process.MainModule?.FileName ?? process.ProcessName; }
            catch { exePath = process.ProcessName; }

            return new AppInfo
            {
                Name = GetProcessDisplayName(process),
                ExecutablePath = exePath,
                IsRunning = true,
                ProcessId = process.Id,
                WindowTitle = process.MainWindowTitle
            };
        }
        catch { return null; }
    }

    public async Task<List<AppInfo>> GetAllAppsAsync()
    {
        var installed = await ScanInstalledAppsAsync();
        var running = await GetRunningAppsAsync();

        // Merge: mark installed apps as running if they are
        foreach (var app in installed)
        {
            var runningMatch = running.FirstOrDefault(r =>
                r.ExecutablePath.Equals(app.ExecutablePath, StringComparison.OrdinalIgnoreCase)
                || r.Name.Equals(app.Name, StringComparison.OrdinalIgnoreCase));

            if (runningMatch != null)
            {
                app.IsRunning = true;
                app.ProcessId = runningMatch.ProcessId;
                app.WindowTitle = runningMatch.WindowTitle;
            }
        }

        // Add running apps that weren't in installed list
        foreach (var runApp in running)
        {
            if (!installed.Any(i =>
                i.ExecutablePath.Equals(runApp.ExecutablePath, StringComparison.OrdinalIgnoreCase)
                || i.Name.Equals(runApp.Name, StringComparison.OrdinalIgnoreCase)))
            {
                installed.Add(runApp);
            }
        }

        var result = installed.OrderBy(a => a.Name).ToList();
        ScanCompleted?.Invoke(this, result);
        return result;
    }
}
