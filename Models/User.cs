using System;

namespace DesktopAssistant.Models;

/// <summary>
/// Represents the authenticated user.
/// </summary>
public class User
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime TokenExpiry { get; set; }
}
