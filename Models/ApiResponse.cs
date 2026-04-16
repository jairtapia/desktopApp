using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DesktopAssistant.Models;

/// <summary>
/// Generic API response wrapper.
/// </summary>
public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("errors")]
    public List<string>? Errors { get; set; }
}

/// <summary>
/// Login/Register response data.
/// </summary>
public class AuthResponseData
{
    [JsonPropertyName("access_token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public UserData? User { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

public class UserData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
