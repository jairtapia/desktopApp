using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DesktopAssistant.Helpers;
using DesktopAssistant.Models;

namespace DesktopAssistant.Services;

/// <summary>
/// Implementation of IAuthService using REST API calls.
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private User? _currentUser;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public bool IsAuthenticated => _currentUser != null && !string.IsNullOrEmpty(_currentUser.Token);
    public User? CurrentUser => _currentUser;
    public event EventHandler<bool>? AuthStateChanged;

    public AuthService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(SecureStorage.GetApiBaseUrl())
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        // Try to restore session
        RestoreSession();
    }

    public async Task<ApiResponse<AuthResponseData>> LoginAsync(string email, string password)
    {
        try
        {
            var payload = new { email, password };
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync("/api/v1/auth/login", content);
            var json = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var data = JsonSerializer.Deserialize<AuthResponseData>(json, _jsonOptions);
                if (data != null)
                {
                    SaveSession(data);
                    return new ApiResponse<AuthResponseData> { Success = true, Data = data };
                }
            }

            return new ApiResponse<AuthResponseData>
            {
                Success = false,
                Message = "Login failed. Please check your credentials."
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<AuthResponseData>
            {
                Success = false,
                Message = $"Connection error: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<AuthResponseData>> RegisterAsync(string email, string password, string name)
    {
        try
        {
            var payload = new { email, password, name };
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync("/api/v1/auth/register", content);
            var json = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var data = JsonSerializer.Deserialize<AuthResponseData>(json, _jsonOptions);
                if (data != null)
                {
                    SaveSession(data);
                    return new ApiResponse<AuthResponseData> { Success = true, Data = data };
                }
            }

            return new ApiResponse<AuthResponseData>
            {
                Success = false,
                Message = "Registration failed."
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<AuthResponseData>
            {
                Success = false,
                Message = $"Connection error: {ex.Message}"
            };
        }
    }

    public async Task<bool> LogoutAsync()
    {
        try
        {
            if (IsAuthenticated)
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _currentUser!.Token);
                await _httpClient.PostAsync("/api/v1/auth/logout", null);
            }
        }
        catch
        {
            // Ignore network errors on logout
        }
        finally
        {
            ClearSession();
        }

        return true;
    }

    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var refreshToken = SecureStorage.GetRefreshToken();
            if (string.IsNullOrEmpty(refreshToken)) return false;

            var payload = new { refreshToken };
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync("/api/v1/auth/refresh", content);
            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<AuthResponseData>(json, _jsonOptions);

            if (data != null)
            {
                SaveSession(data);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RegisterPairingCodeAsync(string code)
    {
        try
        {
            if (!IsAuthenticated) return false;

            var payload = new { code };
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _currentUser!.Token);

            var response = await _httpClient.PostAsync("/api/v1/auth/register-pair", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void SaveSession(AuthResponseData data)
    {
        _currentUser = new User
        {
            Id = data.User?.Id ?? "",
            Email = data.User?.Email ?? "",
            Name = data.User?.Name ?? "",
            Token = data.Token,
            RefreshToken = data.RefreshToken,
            TokenExpiry = DateTime.UtcNow.AddSeconds(data.ExpiresIn)
        };

        SecureStorage.SaveAuthToken(data.Token);
        SecureStorage.SaveRefreshToken(data.RefreshToken);
        SecureStorage.SaveUserData(_currentUser);

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", data.Token);

        AuthStateChanged?.Invoke(this, true);
    }

    private void RestoreSession()
    {
        var user = SecureStorage.GetUserData();
        if (user != null && !string.IsNullOrEmpty(user.Token))
        {
            _currentUser = user;
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", user.Token);
        }
    }

    private void ClearSession()
    {
        _currentUser = null;
        SecureStorage.ClearAuth();
        _httpClient.DefaultRequestHeaders.Authorization = null;
        AuthStateChanged?.Invoke(this, false);
    }
}
