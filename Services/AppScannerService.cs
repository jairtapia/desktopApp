using System.Diagnostics;
using System;
using System.Collections.Generic;
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

    public async Task<List<AppInfo>> ScanInstalledAppsAsync()
    {
        return await Task.Run(() =>
        {
            var apps = new List<AppInfo>();
            var registryPaths = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (var path in registryPaths)
            {
                using var key = Registry.LocalMachine.OpenSubKey(path);
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

                        // Clean up exe path (remove icon index like ",0")
                        if (exePath.Contains(','))
                            exePath = exePath[..exePath.LastIndexOf(',')];

                        // Remove quotes
                        exePath = exePath.Trim('"');

                        var app = new AppInfo
                        {
                            Name = displayName,
                            ExecutablePath = exePath,
                            Publisher = subKey.GetValue("Publisher") as string,
                            Version = subKey.GetValue("DisplayVersion") as string,
                            IconPath = subKey.GetValue("DisplayIcon") as string,
                            IsRunning = false
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
                        Name = process.ProcessName,
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
