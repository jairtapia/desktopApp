using System.Text.Json;
using System.IO;

namespace DesktopAssistant.Helpers;

/// <summary>
/// Provides local storage for auth tokens and app settings.
/// Uses a JSON file in the local application data folder.
/// </summary>
public static class SecureStorage
{
    private static readonly string StoragePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DesktopAssistant",
        "settings.json");

    private static Dictionary<string, string> _settings = new();

    static SecureStorage()
    {
        LoadSettings();
    }

    private static void LoadSettings()
    {
        try
        {
            if (File.Exists(StoragePath))
            {
                var json = File.ReadAllText(StoragePath);
                _settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }
        }
        catch
        {
            _settings = new();
        }
    }

    private static void SaveSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(StoragePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var json = JsonSerializer.Serialize(_settings);
            File.WriteAllText(StoragePath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    private const string AuthTokenKey = "auth_token";
    private const string RefreshTokenKey = "refresh_token";
    private const string UserDataKey = "user_data";
    private const string TutorialCompletedKey = "tutorial_completed";
    private const string ApiBaseUrlKey = "api_base_url";
    private const string WsBaseUrlKey = "ws_base_url";
    private const string PairedDevicesKey = "paired_devices";

    #region Auth

    public static void SaveAuthToken(string token)
    {
        _settings[AuthTokenKey] = token;
        SaveSettings();
    }

    public static string? GetAuthToken()
    {
        return _settings.TryGetValue(AuthTokenKey, out var value) ? value : null;
    }

    public static void SaveRefreshToken(string token)
    {
        _settings[RefreshTokenKey] = token;
        SaveSettings();
    }

    public static string? GetRefreshToken()
    {
        return _settings.TryGetValue(RefreshTokenKey, out var value) ? value : null;
    }

    public static void SaveUserData(Models.User user)
    {
        var json = JsonSerializer.Serialize(user);
        _settings[UserDataKey] = json;
        SaveSettings();
    }

    public static Models.User? GetUserData()
    {
        if (!_settings.TryGetValue(UserDataKey, out var json)) 
            return null;
        
        try
        {
            return JsonSerializer.Deserialize<Models.User>(json);
        }
        catch
        {
            return null;
        }
    }

    public static void ClearAuth()
    {
        _settings.Remove(AuthTokenKey);
        _settings.Remove(RefreshTokenKey);
        _settings.Remove(UserDataKey);
        SaveSettings();
    }

    public static bool HasValidSession()
    {
        var token = GetAuthToken();
        return !string.IsNullOrEmpty(token);
    }

    #endregion

    #region Tutorial

    public static void SetTutorialCompleted(bool completed = true)
    {
        _settings[TutorialCompletedKey] = completed.ToString();
        SaveSettings();
    }

    public static bool IsTutorialCompleted()
    {
        return _settings.TryGetValue(TutorialCompletedKey, out var value)
            && bool.TryParse(value, out var b) && b;
    }

    #endregion

    #region Configuration

    public static void SaveApiBaseUrl(string url)
    {
        _settings[ApiBaseUrlKey] = url;
        SaveSettings();
    }

    public static string GetApiBaseUrl()
    {
        return (_settings.TryGetValue(ApiBaseUrlKey, out var value) ? value : null) ?? "http://localhost:8000";
    }

    public static void SaveWsBaseUrl(string url)
    {
        _settings[WsBaseUrlKey] = url;
        SaveSettings();
    }

    public static string GetWsBaseUrl()
    {
        return (_settings.TryGetValue(WsBaseUrlKey, out var value) ? value : null) ?? "ws://localhost:8000/ws";
    }

    #endregion

    #region General

    public static void Set(string key, string value)
    {
        _settings[key] = value;
        SaveSettings();
    }

    public static string? Get(string key)
    {
        return _settings.TryGetValue(key, out var value) ? value : null;
    }

    public static void Remove(string key)
    {
        if (_settings.Remove(key))
        {
            SaveSettings();
        }
    }

    #endregion
}
