using DesktopAssistant.Models;

namespace DesktopAssistant.Services;

/// <summary>
/// Service for managing device pairing via QR codes.
/// </summary>
public interface IDevicePairingService
{
    DevicePairInfo GeneratePairInfo();
    byte[] GenerateQrCode(DevicePairInfo? pairInfo = null);
    bool ValidatePairToken(string token);
}
