using System;

namespace DesktopAssistant.Models;

/// <summary>
/// Represents an installed or running application on the system.
/// </summary>
public class AppInfo
{
    public string Name { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string? IconPath { get; set; }
    public string? Publisher { get; set; }
    public string? Version { get; set; }
    public bool IsRunning { get; set; }
    public int? ProcessId { get; set; }
    public string? WindowTitle { get; set; }
    public DateTime? InstallDate { get; set; }

    /// <summary>
    /// Unique identifier for the app (based on exe path hash).
    /// </summary>
    public string Id => Convert.ToBase64String(
        System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(
                (string.IsNullOrWhiteSpace(ExecutablePath) ? Name : ExecutablePath).ToLowerInvariant()
            )
        )
    )[..12];
}
