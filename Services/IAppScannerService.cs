using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DesktopAssistant.Models;

namespace DesktopAssistant.Services;

/// <summary>
/// Service for scanning installed and running applications.
/// </summary>
public interface IAppScannerService
{
    Task<List<AppInfo>> ScanInstalledAppsAsync();
    Task<List<AppInfo>> GetRunningAppsAsync();
    Task<List<AppInfo>> GetAllAppsAsync();
    AppInfo? GetForegroundApp();
    event EventHandler<List<AppInfo>>? ScanCompleted;
}
