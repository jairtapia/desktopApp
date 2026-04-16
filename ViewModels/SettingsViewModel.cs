using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopAssistant.Helpers;
using DesktopAssistant.Services;

namespace DesktopAssistant.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IWebSocketService _webSocketService;
    private readonly NavigationService _navigationService;

    [ObservableProperty]
    public partial string ApiBaseUrl { get; set; }

    [ObservableProperty]
    public partial string WsBaseUrl { get; set; }

    [ObservableProperty]
    public partial string UserName { get; set; }

    [ObservableProperty]
    public partial string UserEmail { get; set; }

    [ObservableProperty]
    public partial bool IsConnected { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public SettingsViewModel(
        IAuthService authService,
        IWebSocketService webSocketService,
        NavigationService navigationService)
    {
        _authService = authService;
        _webSocketService = webSocketService;
        _navigationService = navigationService;

        ApiBaseUrl = SecureStorage.GetApiBaseUrl();
        WsBaseUrl = SecureStorage.GetWsBaseUrl();
        UserName = _authService.CurrentUser?.Name ?? "";
        UserEmail = _authService.CurrentUser?.Email ?? "";
        IsConnected = _webSocketService.IsConnected;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        SecureStorage.SaveApiBaseUrl(ApiBaseUrl);
        SecureStorage.SaveWsBaseUrl(WsBaseUrl);
        StatusMessage = "Settings saved successfully";
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _webSocketService.DisconnectAsync();
        await _authService.LogoutAsync();
        SecureStorage.SetTutorialCompleted(false);
        _navigationService.NavigateRoot(typeof(Views.LoginPage));
    }

    [RelayCommand]
    private void ResetTutorial()
    {
        SecureStorage.SetTutorialCompleted(false);
        StatusMessage = "Tutorial will show on next login";
    }
}
