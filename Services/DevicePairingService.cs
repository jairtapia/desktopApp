using DesktopAssistant.Helpers;
using DesktopAssistant.Models;

namespace DesktopAssistant.Services;

/// <summary>
/// Manages device pairing with QR code generation and token validation.
/// </summary>
public class DevicePairingService : IDevicePairingService
{
    private DevicePairInfo? _currentPairInfo;

    public DevicePairInfo GeneratePairInfo()
    {
        _currentPairInfo = new DevicePairInfo
        {
            LocalIp = NetworkHelper.GetLocalIpAddress(),
            Port = 8765,
            PairToken = TokenHelper.GeneratePairToken(),
            DeviceName = NetworkHelper.GetHostName(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        return _currentPairInfo;
    }

    public byte[] GenerateQrCode(DevicePairInfo? pairInfo = null)
    {
        var info = pairInfo ?? _currentPairInfo ?? GeneratePairInfo();
        return QrCodeGenerator.GenerateQrCodePng(info);
    }

    public bool ValidatePairToken(string token)
    {
        if (_currentPairInfo == null) return false;
        if (_currentPairInfo.IsExpired) return false;
        return _currentPairInfo.PairToken == token;
    }
}
