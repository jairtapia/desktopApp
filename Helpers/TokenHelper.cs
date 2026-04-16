using System.Security.Cryptography;

namespace DesktopAssistant.Helpers;

/// <summary>
/// Generates secure pairing tokens for device linking.
/// </summary>
public static class TokenHelper
{
    /// <summary>
    /// Generates a cryptographically secure random token.
    /// </summary>
    /// <param name="length">Length of the token string (default 32).</param>
    /// <returns>A URL-safe base64-encoded random token.</returns>
    public static string GeneratePairToken(int length = 32)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "")
            [..Math.Min(length, Convert.ToBase64String(bytes).Length)];
    }

    /// <summary>
    /// Generates a short 6-digit numeric code for display pairing.
    /// </summary>
    public static string GenerateShortCode()
    {
        var code = RandomNumberGenerator.GetInt32(100000, 999999);
        return code.ToString();
    }
}
