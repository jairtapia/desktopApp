using System;

namespace DesktopAssistant.Models;

/// <summary>
/// Contains the information encoded in the QR code for device pairing.
/// </summary>
public class DevicePairInfo
{
    public string LocalIp { get; set; } = string.Empty;
    public int Port { get; set; } = 8765;
    public string PairToken { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(5);

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}
