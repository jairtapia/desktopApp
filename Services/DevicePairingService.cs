using DesktopAssistant.Helpers;
using DesktopAssistant.Models;

namespace DesktopAssistant.Services;

/// <summary>
/// Manages device pairing with QR code generation and token validation.
/// </summary>
public class DevicePairingService : IDevicePairingService
{
    private DevicePairInfo? _currentPairInfo;

    private static string NormalizeUrlForPairing(
        string configuredUrl,
        string localIp,
        string fallbackScheme,
        int fallbackPort,
        string fallbackPath)
    {
        if (Uri.TryCreate(configuredUrl, UriKind.Absolute, out var uri))
        {
            var builder = new UriBuilder(uri);
            if (builder.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || builder.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || builder.Host.Equals("::1", StringComparison.OrdinalIgnoreCase))
            {
                builder.Host = localIp;
            }

            return builder.Uri.ToString().TrimEnd('/');
        }

        return $"{fallbackScheme}://{localIp}:{fallbackPort}{fallbackPath}";
    }

    public DevicePairInfo GeneratePairInfo()
    {
        var localIp = NetworkHelper.GetLocalIpAddress();
        var apiBaseUrl = NormalizeUrlForPairing(
            SecureStorage.GetApiBaseUrl(),
            localIp,
            "http",
            8000,
            "/api/v1");
        var wsBaseUrl = NormalizeUrlForPairing(
            SecureStorage.GetWsBaseUrl(),
            localIp,
            "ws",
            8000,
            "/ws");
        var wsPort = 8765;

        if (Uri.TryCreate(wsBaseUrl, UriKind.Absolute, out var wsUri) && wsUri.Port > 0)
        {
            wsPort = wsUri.Port;
        }

        _currentPairInfo = new DevicePairInfo
        {
            LocalIp = localIp,
            Port = wsPort,
            PairToken = TokenHelper.GeneratePairToken(),
            DeviceName = NetworkHelper.GetHostName(),
            ApiBaseUrl = apiBaseUrl,
            WsBaseUrl = wsBaseUrl,
            SyncVersion = "1.0",
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
