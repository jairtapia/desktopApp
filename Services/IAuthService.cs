using System;
using System.Threading.Tasks;
using DesktopAssistant.Models;

namespace DesktopAssistant.Services;

/// <summary>
/// Authentication service for login, register, and token management.
/// </summary>
public interface IAuthService
{
    Task<ApiResponse<AuthResponseData>> LoginAsync(string email, string password);
    Task<ApiResponse<AuthResponseData>> RegisterAsync(string email, string password, string name);
    Task<bool> LogoutAsync();
    Task<bool> RefreshTokenAsync();
    Task<bool> RegisterPairingCodeAsync(string code);
    bool IsAuthenticated { get; }
    User? CurrentUser { get; }
    event EventHandler<bool>? AuthStateChanged;
}
