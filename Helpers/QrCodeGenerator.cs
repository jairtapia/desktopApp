using QRCoder;
using System.Text.Json;
using DesktopAssistant.Models;

namespace DesktopAssistant.Helpers;

/// <summary>
/// Generates QR codes for device pairing.
/// </summary>
public static class QrCodeGenerator
{
    /// <summary>
    /// Generates a QR code as a PNG byte array from DevicePairInfo.
    /// </summary>
    public static byte[] GenerateQrCodePng(DevicePairInfo pairInfo, int pixelsPerModule = 10)
    {
        var payload = JsonSerializer.Serialize(new
        {
            ip = pairInfo.LocalIp,
            port = pairInfo.Port,
            token = pairInfo.PairToken,
            device = pairInfo.DeviceName,
            expires = pairInfo.ExpiresAt.ToString("o"),
            apiBaseUrl = pairInfo.ApiBaseUrl,
            wsBaseUrl = pairInfo.WsBaseUrl,
            syncVersion = pairInfo.SyncVersion,
        });

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);

        return qrCode.GetGraphic(pixelsPerModule);
    }

    /// <summary>
    /// Generates a QR code as a PNG byte array from a raw string.
    /// </summary>
    public static byte[] GenerateQrCodePng(string content, int pixelsPerModule = 10)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);

        return qrCode.GetGraphic(pixelsPerModule);
    }
}
